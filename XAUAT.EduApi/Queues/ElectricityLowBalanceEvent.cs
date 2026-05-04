namespace XAUAT.EduApi.Queues;

public sealed record ElectricityLowBalanceEvent(
    string SubscriptionId,
    string ElectricityUrl,
    string Email,
    double Threshold,
    double Balance);
