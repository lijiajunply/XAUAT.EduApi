using EduApi.Data.Models;
using Microsoft.Extensions.Options;
using XAUAT.EduApi.Configuration;
using XAUAT.EduApi.Repos;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Queues;

public sealed class ElectricitySubscriptionMonitorBackgroundService(
    IServiceScopeFactory scopeFactory,
    IElectricityNotificationQueue queue,
    IOptions<ElectricitySubscriptionOptions> options,
    ILogger<ElectricitySubscriptionMonitorBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueSubscriptionsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "执行电费订阅扫描任务时出错");
            }

            var delay = GetPollInterval();
            await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessDueSubscriptionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IElectricitySubscriptionRepository>();
            var electricityService = scope.ServiceProvider.GetRequiredService<IElectricityService>();
            var currentOptions = options.Value;
            var now = DateTime.UtcNow;

            var subscriptions = await repository
                .GetDueSubscriptionsAsync(now, currentOptions.BatchSize, cancellationToken)
                .ConfigureAwait(false);

            if (subscriptions.Count == 0)
            {
                return;
            }

            foreach (var subscription in subscriptions)
            {
                await ProcessSubscriptionAsync(repository, electricityService, subscription, currentOptions, now,
                    cancellationToken).ConfigureAwait(false);
            }

            if (subscriptions.Count < currentOptions.BatchSize)
            {
                return;
            }
        }
    }

    private async Task ProcessSubscriptionAsync(
        IElectricitySubscriptionRepository repository,
        IElectricityService electricityService,
        ElectricitySubscription subscription,
        ElectricitySubscriptionOptions currentOptions,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var previousBalance = subscription.LastKnownBalance;

        try
        {
            var currentBalance = await electricityService.FetchCurrentBalanceAsync(subscription.ElectricityUrl)
                .ConfigureAwait(false);

            subscription.LastCheckedAt = now;
            subscription.UpdatedAt = now;
            subscription.NextCheckAt = now.AddMinutes(currentOptions.ScanIntervalMinutes);

            if (!currentBalance.HasValue)
            {
                subscription.LastErrorMessage = "无法获取电费余额";
                await repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
                return;
            }

            subscription.LastKnownBalance = currentBalance.Value;
            subscription.LastErrorMessage = "";

            if (ShouldNotify(subscription, currentBalance.Value, previousBalance, currentOptions, now))
            {
                await queue.QueueAsync(new ElectricityLowBalanceEvent(
                    subscription.Id,
                    subscription.ElectricityUrl,
                    subscription.Email,
                    subscription.Threshold,
                    currentBalance.Value), cancellationToken).ConfigureAwait(false);
            }

            await repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            subscription.LastCheckedAt = now;
            subscription.UpdatedAt = now;
            subscription.NextCheckAt = now.AddMinutes(currentOptions.ScanIntervalMinutes);
            subscription.LastErrorMessage = ex.Message;

            await repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
            logger.LogWarning(ex, "扫描电费订阅失败，SubscriptionId: {SubscriptionId}", subscription.Id);
        }
    }

    private static bool ShouldNotify(ElectricitySubscription subscription, double currentBalance, double? previousBalance,
        ElectricitySubscriptionOptions currentOptions, DateTime now)
    {
        if (currentBalance >= subscription.Threshold)
        {
            return false;
        }

        if (!subscription.LastAlertedAt.HasValue)
        {
            return true;
        }

        if (previousBalance.HasValue && previousBalance.Value >= subscription.Threshold)
        {
            return true;
        }

        var cooldown = TimeSpan.FromMinutes(currentOptions.NotificationCooldownMinutes);
        return now - subscription.LastAlertedAt.Value >= cooldown;
    }

    private TimeSpan GetPollInterval()
    {
        var configured = options.Value.ScanIntervalMinutes;
        return TimeSpan.FromMinutes(Math.Max(1, Math.Min(configured, 5)));
    }
}
