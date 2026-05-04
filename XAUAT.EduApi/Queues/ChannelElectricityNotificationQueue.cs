using System.Threading.Channels;

namespace XAUAT.EduApi.Queues;

public sealed class ChannelElectricityNotificationQueue : IElectricityNotificationQueue
{
    private const int Capacity = 1_000;
    private readonly Channel<ElectricityLowBalanceEvent> _channel = Channel.CreateBounded<ElectricityLowBalanceEvent>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask QueueAsync(ElectricityLowBalanceEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(notificationEvent, cancellationToken);
    }

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.WaitToReadAsync(cancellationToken);
    }

    public bool TryRead(out ElectricityLowBalanceEvent? notificationEvent)
    {
        return _channel.Reader.TryRead(out notificationEvent);
    }
}
