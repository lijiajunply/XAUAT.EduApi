using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduApi.Data.Models;

[Table("electricity_notification_logs")]
public class ElectricityNotificationLog
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [MaxLength(64)]
    public string SubscriptionId { get; set; } = "";

    [ForeignKey(nameof(SubscriptionId))]
    public ElectricitySubscription? Subscription { get; set; }

    [MaxLength(256)]
    public string Email { get; set; } = "";

    public double Threshold { get; set; }

    public double Balance { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsSuccess { get; set; }

    [MaxLength(1024)]
    public string Message { get; set; } = "";
}
