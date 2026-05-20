namespace RunRoutes.Core.Users;

public interface IEmailService
{
    Task SendActivationEmailAsync(string email, string token);
    Task SendEmailChangeEmailAsync(string email, string token);
}
