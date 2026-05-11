using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Users;

namespace RunRoutes.Infrastructure.Services;

public class EmailService(
    IResend resend,
    IOptions<EmailSettings> options,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendActivationEmailAsync(string email, string token)
    {
        var link = $"{_settings.FrontendBaseUrl}/activate?token={token}";
        var message = new EmailMessage
        {
            From = _settings.FromAddress,
            To = email,
            Subject = "【RunRoutes】アカウント有効化のご案内",
            HtmlBody = $"""
                <p>RunRoutes にご登録いただきありがとうございます。</p>
                <p>以下のリンクからアカウントを有効化してください（有効期限: 24 時間）。</p>
                <p><a href="{link}">アカウントを有効化する</a></p>
                <p>リンクが開かない場合は次の URL を直接ブラウザに貼り付けてください:<br>{link}</p>
                """,
        };
        await resend.EmailSendAsync(message);
        logger.LogInformation("アカウント有効化メール送信完了 → {Email}", email);
    }

    public async Task SendEmailChangeEmailAsync(string email, string token)
    {
        var link = $"{_settings.FrontendBaseUrl}/activate-email?token={token}";
        var message = new EmailMessage
        {
            From = _settings.FromAddress,
            To = email,
            Subject = "【RunRoutes】メールアドレス変更の確認",
            HtmlBody = $"""
                <p>RunRoutes のメールアドレス変更リクエストを受け付けました。</p>
                <p>以下のリンクから変更を確定してください（有効期限: 24 時間）。</p>
                <p><a href="{link}">メールアドレス変更を確定する</a></p>
                <p>心当たりがない場合はこのメールを無視してください。</p>
                """,
        };
        await resend.EmailSendAsync(message);
        logger.LogInformation("メール変更確認メール送信完了 → {Email}", email);
    }
}
