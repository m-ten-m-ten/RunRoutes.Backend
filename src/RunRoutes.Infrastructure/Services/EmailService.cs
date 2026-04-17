using Microsoft.Extensions.Logging;
using RunRoutes.Core.Interfaces.Services;

namespace RunRoutes.Infrastructure.Services;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendActivationEmailAsync(string email, string token)
    {
        logger.LogInformation("アカウント有効化メール送信 → {Email}, token: {Token}", email, token);
        return Task.CompletedTask;
    }

    public Task SendEmailChangeEmailAsync(string email, string token)
    {
        logger.LogInformation("メール変更確認メール送信 → {Email}, token: {Token}", email, token);
        return Task.CompletedTask;
    }
}
