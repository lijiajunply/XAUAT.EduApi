using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
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

        var bodyBuilder = new BodyBuilder
        {
            TextBody = BuildBody(notificationEvent, rechargeUrl),
            HtmlBody = BuildHtmlBody(notificationEvent, rechargeUrl)
        };
        message.Body = bodyBuilder.ToMessageBody();

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

    private static string BuildHtmlBody(ElectricityLowBalanceEvent notificationEvent, string? rechargeUrl)
    {
        var rechargeActionHtml = !string.IsNullOrEmpty(rechargeUrl)
            ? $"<div class=\"actions\"><a href=\"{rechargeUrl}\" class=\"btn\">立即充值</a></div>"
            : "<div class=\"actions\"><p style=\"color: #64748b; font-size: 14px; text-align: center;\">未获取到自动充值链接，请自行前往充值页面。</p></div>";
            
        return
            $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <style>
                    body {
                        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
                        line-height: 1.6;
                        color: #333333;
                        background-color: #f4f6f8;
                        margin: 0;
                        padding: 0;
                    }
                    .container {
                        max-width: 600px;
                        margin: 40px auto;
                        background-color: #ffffff;
                        border-radius: 8px;
                        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
                        overflow: hidden;
                    }
                    .header {
                        background-color: #4f46e5;
                        color: #ffffff;
                        padding: 24px;
                        text-align: center;
                    }
                    .header h1 {
                        margin: 0;
                        font-size: 24px;
                        font-weight: 600;
                    }
                    .content {
                        padding: 32px 24px;
                    }
                    .greeting {
                        font-size: 16px;
                        margin-bottom: 24px;
                    }
                    .info-card {
                        background-color: #f8fafc;
                        border: 1px solid #e2e8f0;
                        border-radius: 6px;
                        padding: 20px;
                        margin-bottom: 24px;
                    }
                    .info-item {
                        display: flex;
                        justify-content: space-between;
                        margin-bottom: 12px;
                        border-bottom: 1px dashed #e2e8f0;
                        padding-bottom: 8px;
                    }
                    .info-item:last-child {
                        margin-bottom: 0;
                        border-bottom: none;
                        padding-bottom: 0;
                    }
                    .info-label {
                        color: #64748b;
                        font-weight: 500;
                    }
                    .info-value {
                        font-weight: 600;
                        color: #0f172a;
                    }
                    .balance-warning {
                        color: #ef4444;
                        font-size: 24px;
                    }
                    .actions {
                        text-align: center;
                        margin-top: 32px;
                    }
                    .btn {
                        display: inline-block;
                        background-color: #4f46e5;
                        color: #ffffff;
                        text-decoration: none;
                        padding: 12px 28px;
                        border-radius: 4px;
                        font-weight: 500;
                    }
                    .footer {
                        background-color: #f8fafc;
                        color: #94a3b8;
                        text-align: center;
                        padding: 16px;
                        font-size: 14px;
                        border-top: 1px solid #e2e8f0;
                    }
                    .link {
                        color: #4f46e5;
                        text-decoration: none;
                        word-break: break-all;
                    }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>电费余额不足提醒</h1>
                    </div>
                    <div class="content">
                        <div class="greeting">你好，<br><br>这是来自 XAUAT EduApi 的电费余额提醒。您的寝室电费余额已低于设定的提醒阈值。</div>
                        
                        <div class="info-card">
                            <div class="info-item">
                                <span class="info-label">当前电费余额</span>
                                <span class="info-value balance-warning">{{notificationEvent.Balance:F2}} 元</span>
                            </div>
                            <div class="info-item">
                                <span class="info-label">提醒阈值</span>
                                <span class="info-value">{{notificationEvent.Threshold:F2}} 元</span>
                            </div>
                            <div class="info-item" style="flex-direction: column;">
                                <span class="info-label" style="margin-bottom: 8px;">电费页面</span>
                                <a href="{{notificationEvent.ElectricityUrl}}" class="link" style="font-size: 14px; font-weight: normal;">{{notificationEvent.ElectricityUrl}}</a>
                            </div>
                        </div>

                        {{rechargeActionHtml}}
                        
                        <p style="color: #64748b; font-size: 14px; text-align: center; margin-top: 24px;">请尽快检查并充值，避免影响正常用电。</p>
                    </div>
                    <div class="footer">
                        <p style="margin: 0;">此邮件由 XAUAT EduApi 自动发送，请勿直接回复。</p>
                    </div>
                </div>
            </body>
            </html>
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
