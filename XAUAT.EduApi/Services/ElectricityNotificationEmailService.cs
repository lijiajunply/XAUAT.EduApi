using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
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

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(notificationEvent.Email));
        message.Subject = $"[XAUAT EduApi] 电费余额提醒：当前余额 {notificationEvent.Balance:F2} 元";
        message.Body = new TextPart(TextFormat.Plain)
        {
            Text = BuildBody(notificationEvent, rechargeUrl)
        };

        using var client = new SmtpClient();

        cancellationToken.ThrowIfCancellationRequested();

        await client.ConnectAsync(
                options.Host,
                options.Port,
                GetSecureSocketOptions(options),
                cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(options.UserName))
        {
            await client.AuthenticateAsync(options.UserName, options.Password, cancellationToken)
                .ConfigureAwait(false);
        }

        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
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

    private static SecureSocketOptions GetSecureSocketOptions(SmtpOptions options)
    {
        if (!options.EnableSsl)
        {
            return SecureSocketOptions.None;
        }

        return options.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }
}
