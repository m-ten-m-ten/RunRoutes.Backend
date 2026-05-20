using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Exceptions;

namespace RunRoutes.Core.Users;

public sealed record EmailChangeRequest
{
    public EmailAddress NewEmail { get; }
    public string Token { get; }
    public DateTime ExpiresAt { get; }

    private EmailChangeRequest(EmailAddress newEmail, string token, DateTime expiresAt)
    {
        NewEmail = newEmail;
        Token = token;
        ExpiresAt = expiresAt;
    }

    public static EmailChangeRequest Create(EmailAddress newEmail, DateTime now, TimeSpan validity)
    {
        if (validity <= TimeSpan.Zero)
            throw new ValidationException("有効期間は正の値である必要があります");

        var token = SecureToken.Generate();
        return new EmailChangeRequest(newEmail, token, now + validity);
    }

    public bool IsExpired(DateTime now) => now >= ExpiresAt;
}
