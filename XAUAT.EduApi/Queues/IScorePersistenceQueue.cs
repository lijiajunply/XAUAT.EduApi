namespace XAUAT.EduApi.Queues;

public interface IScorePersistenceQueue
{
    ValueTask QueueAsync(ScoreCrawledEvent scoreCrawledEvent, CancellationToken cancellationToken = default);
    ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default);
    bool TryRead(out ScoreCrawledEvent? scoreCrawledEvent);
}
