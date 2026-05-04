using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using XAUAT.EduApi.Configuration;
using XAUAT.EduApi.Queues;

namespace XAUAT.EduApi.Services;

public interface IElectricityNotificationEmailService
{
    Task SendLowBalanceAlertAsync(ElectricityLowBalanceEvent notificationEvent,
        CancellationToken cancellationToken = default);
}

public class ElectricityNotificationEmailService(
    IOptions<SmtpOptions> smtpOptions,
    IElectricityService electricityService)
    : IElectricityNotificationEmailService
{
    public async Task SendLowBalanceAlertAsync(ElectricityLowBalanceEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        var options = smtpOptions.Value;
        ValidateOptions(options);

        var rechargeUrl = await electricityService.GetRechargeUrlAsync(notificationEvent.ElectricityUrl)
            .ConfigureAwait(false);

        using var message = new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromName),
            Subject = $"[XAUAT EduApi] 电费余额提醒：当前余额 {notificationEvent.Balance:F2} 元",
            Body = BuildBody(notificationEvent, rechargeUrl),
            IsBodyHtml = false
        };

        message.To.Add(notificationEvent.Email);

        using var client = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(options.UserName))
        {
            client.Credentials = new NetworkCredential(options.UserName, options.Password);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message).ConfigureAwait(false);
    }

    private static string BuildBody(ElectricityLowBalanceEvent notificationEvent, string? rechargeUrl)
    {
        return
            $"""
             你好，

             这是 XAUAT EduApi 发送的电费余额提醒。

             当前电费余额：{notificationEvent.Balance:F2} 元
             提醒阈值：{notificationEvent.Threshold:F2} 元
             电费页面：{notificationEvent.ElectricityUrl}
             充值地址：{rechargeUrl ?? "未能自动生成充值地址"}

             请尽快检查并充值，避免影响正常使用。
             """;
    }

    private static void ValidateOptions(SmtpOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
        {
            throw new InvalidOperationException("SMTP Host 未配置");
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            throw new InvalidOperationException("SMTP FromAddress 未配置");
        }
    }
}
