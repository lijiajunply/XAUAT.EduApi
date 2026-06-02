using XAUAT.EduApi.Repos;

namespace XAUAT.EduApi.Services;

public sealed class ExamCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExamCleanupBackgroundService> logger)
    : BackgroundService
{
    private const int RetentionDays = 30;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

                using var scope = scopeFactory.CreateScope();
                var examRepository = scope.ServiceProvider.GetRequiredService<IExamRepository>();
                var deletedCount = await examRepository.DeleteExpiredAsync(cutoff);

                if (deletedCount > 0)
                {
                    logger.LogInformation("清理过期考试记录完成，截止时间: {Cutoff}，删除数量: {Count}", cutoff, deletedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "清理过期考试记录时出错");
            }

            try
            {
                await Task.Delay(CleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
