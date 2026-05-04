namespace XAUAT.EduApi.Configuration;

public class ElectricitySubscriptionOptions
{
    public const string SectionName = "ElectricitySubscription";

    public int ScanIntervalMinutes { get; set; } = 15;

    public int NotificationCooldownMinutes { get; set; } = 720;

    public int BatchSize { get; set; } = 50;
}
