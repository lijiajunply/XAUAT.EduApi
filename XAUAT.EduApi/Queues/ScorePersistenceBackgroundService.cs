using EduApi.Data.Models;
using XAUAT.EduApi.Repos;

namespace XAUAT.EduApi.Queues;

public sealed class ScorePersistenceBackgroundService(
    IScorePersistenceQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ScorePersistenceBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pendingScores = new List<ScoreResponse>(BatchSize);

        try
        {
            while (await queue.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                DrainQueue(pendingScores);

                if (pendingScores.Count >= BatchSize)
                {
                    await FlushAsync(pendingScores, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                await Task.Delay(FlushInterval, stoppingToken).ConfigureAwait(false);
                DrainQueue(pendingScores);

                if (pendingScores.Count > 0)
                {
                    await FlushAsync(pendingScores, stoppingToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // 应用正在停止，下面尽力刷掉内存中已消费的剩余数据。
        }
        finally
        {
            DrainQueue(pendingScores);
            if (pendingScores.Count > 0)
            {
                await FlushAsync(pendingScores, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private void DrainQueue(List<ScoreResponse> pendingScores)
    {
        while (pendingScores.Count < BatchSize && queue.TryRead(out var scoreCrawledEvent))
        {
            if (scoreCrawledEvent is null)
            {
                continue;
            }

            pendingScores.AddRange(scoreCrawledEvent.Scores);
        }
    }

    private async Task FlushAsync(List<ScoreResponse> pendingScores, CancellationToken cancellationToken)
    {
        var scoresToSave = pendingScores
            .GroupBy(score => score.Key)
            .Select(group => group.First())
            .ToList();

        pendingScores.Clear();

        if (scoresToSave.Count == 0)
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var scoreRepository = scope.ServiceProvider.GetRequiredService<IScoreRepository>();
            await scoreRepository.AddRangeAsync(scoresToSave).ConfigureAwait(false);

            logger.LogInformation("批量保存成绩数据成功，数量: {Count}", scoresToSave.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "批量保存成绩数据到数据库时出错，数量: {Count}", scoresToSave.Count);
        }
    }
}
