using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduApi.Data.Models;

[Table("electricity_subscriptions")]
public class ElectricitySubscription : DataModel
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [MaxLength(1024)]
    public string ElectricityUrl { get; set; } = "";

    [MaxLength(256)]
    public string Email { get; set; } = "";

    public double Threshold { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime NextCheckAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastCheckedAt { get; set; }

    public double? LastKnownBalance { get; set; }

    public DateTime? LastAlertedAt { get; set; }

    public double? LastAlertedBalance { get; set; }

    [MaxLength(512)]
    public string LastErrorMessage { get; set; } = "";

    public ICollection<ElectricityNotificationLog> NotificationLogs { get; set; } =
        new List<ElectricityNotificationLog>();
}
