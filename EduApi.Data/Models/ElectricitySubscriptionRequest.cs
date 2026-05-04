using System.ComponentModel.DataAnnotations;

namespace EduApi.Data.Models;

public class CreateElectricitySubscriptionRequest
{
    [Required(ErrorMessage = "电费 URL 不能为空")]
    [MaxLength(1024)]
    public string Url { get; set; } = "";

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [MaxLength(256)]
    public string Email { get; set; } = "";

    [Range(0.01, double.MaxValue, ErrorMessage = "阈值必须大于 0")]
    public double Threshold { get; set; }
}

public class ElectricitySubscriptionResponse
{
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string Email { get; set; } = "";
    public double Threshold { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime NextCheckAt { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public double? LastKnownBalance { get; set; }
    public DateTime? LastAlertedAt { get; set; }
    public double? LastAlertedBalance { get; set; }
    public string LastErrorMessage { get; set; } = "";

    public static ElectricitySubscriptionResponse FromEntity(ElectricitySubscription subscription)
    {
        return new ElectricitySubscriptionResponse
        {
            Id = subscription.Id,
            Url = subscription.ElectricityUrl,
            Email = subscription.Email,
            Threshold = subscription.Threshold,
            IsActive = subscription.IsActive,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt,
            NextCheckAt = subscription.NextCheckAt,
            LastCheckedAt = subscription.LastCheckedAt,
            LastKnownBalance = subscription.LastKnownBalance,
            LastAlertedAt = subscription.LastAlertedAt,
            LastAlertedBalance = subscription.LastAlertedBalance,
            LastErrorMessage = subscription.LastErrorMessage
        };
    }
}
