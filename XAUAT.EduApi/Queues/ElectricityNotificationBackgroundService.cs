using EduApi.Data.Models;
using XAUAT.EduApi.Repos;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Queues;

public sealed class ElectricityNotificationBackgroundService(
    IElectricityNotificationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ElectricityNotificationBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await queue.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
        {
            while (queue.TryRead(out var notificationEvent))
            {
                if (notificationEvent is null)
                {
                    continue;
                }

                await HandleNotificationAsync(notificationEvent, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleNotificationAsync(ElectricityLowBalanceEvent notificationEvent,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IElectricitySubscriptionRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IElectricityNotificationEmailService>();
        var subscription = await repository.GetByIdAsync(notificationEvent.SubscriptionId, cancellationToken)
            .ConfigureAwait(false);

        if (subscription is null)
        {
            logger.LogWarning("电费订阅不存在，无法发送提醒，SubscriptionId: {SubscriptionId}", notificationEvent.SubscriptionId);
            return;
        }

        var now = DateTime.UtcNow;

        try
        {
            await emailService.SendLowBalanceAlertAsync(notificationEvent, cancellationToken).ConfigureAwait(false);

            subscription.LastAlertedAt = now;
            subscription.LastAlertedBalance = notificationEvent.Balance;
            subscription.LastErrorMessage = "";
            subscription.UpdatedAt = now;
            await repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);

            await repository.AddNotificationLogAsync(new ElectricityNotificationLog
            {
                SubscriptionId = subscription.Id,
                Email = notificationEvent.Email,
                Threshold = notificationEvent.Threshold,
                Balance = notificationEvent.Balance,
                CreatedAt = now,
                IsSuccess = true,
                Message = "邮件发送成功"
            }, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("电费提醒邮件发送成功，SubscriptionId: {SubscriptionId}, Email: {Email}",
                subscription.Id, notificationEvent.Email);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            subscription.LastErrorMessage = ex.Message;
            subscription.UpdatedAt = now;
            await repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);

            await repository.AddNotificationLogAsync(new ElectricityNotificationLog
            {
                SubscriptionId = subscription.Id,
                Email = notificationEvent.Email,
                Threshold = notificationEvent.Threshold,
                Balance = notificationEvent.Balance,
                CreatedAt = now,
                IsSuccess = false,
                Message = ex.Message
            }, cancellationToken).ConfigureAwait(false);

            logger.LogError(ex, "电费提醒邮件发送失败，SubscriptionId: {SubscriptionId}, Email: {Email}",
                subscription.Id, notificationEvent.Email);
        }
    }
}
