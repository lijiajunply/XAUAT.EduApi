using System.Threading.Channels;

namespace XAUAT.EduApi.Queues;

public sealed class ChannelScorePersistenceQueue : IScorePersistenceQueue
{
    private const int Capacity = 1_000;
    private readonly Channel<ScoreCrawledEvent> _channel = Channel.CreateBounded<ScoreCrawledEvent>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask QueueAsync(ScoreCrawledEvent scoreCrawledEvent, CancellationToken cancellationToken = default)
    {
        if (scoreCrawledEvent.Scores.Count == 0)
        {
            return ValueTask.CompletedTask;
        }

        return _channel.Writer.WriteAsync(scoreCrawledEvent, cancellationToken);
    }

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.WaitToReadAsync(cancellationToken);
    }

    public bool TryRead(out ScoreCrawledEvent? scoreCrawledEvent)
    {
        return _channel.Reader.TryRead(out scoreCrawledEvent);
    }
}
