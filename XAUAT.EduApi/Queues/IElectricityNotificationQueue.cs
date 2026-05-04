namespace XAUAT.EduApi.Queues;

public interface IElectricityNotificationQueue
{
    ValueTask QueueAsync(ElectricityLowBalanceEvent notificationEvent, CancellationToken cancellationToken = default);
    ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default);
    bool TryRead(out ElectricityLowBalanceEvent? notificationEvent);
}
