namespace XAUAT.EduApi.Queues;

public sealed class NullScorePersistenceQueue : IScorePersistenceQueue
{
    public static NullScorePersistenceQueue Instance { get; } = new();

    private NullScorePersistenceQueue()
    {
    }

    public ValueTask QueueAsync(ScoreCrawledEvent scoreCrawledEvent, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(false);
    }

    public bool TryRead(out ScoreCrawledEvent? scoreCrawledEvent)
    {
        scoreCrawledEvent = null;
        return false;
    }
}
