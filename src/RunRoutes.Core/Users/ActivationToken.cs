using System.Security.Cryptography;
using RunRoutes.Core.Common.Exceptions;

namespace RunRoutes.Core.Users;

public sealed record ActivationToken
{
    public string Value { get; }
    public DateTime ExpiresAt { get; }

    private ActivationToken(string value, DateTime expiresAt)
    {
        Value = value;
        ExpiresAt = expiresAt;
    }

    public static ActivationToken Generate(DateTime now, TimeSpan validity)
    {
        if (validity <= TimeSpan.Zero)
            throw new ValidationException("有効期間は正の値である必要があります");

        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return new ActivationToken(value, now + validity);
    }

    public static ActivationToken FromStorage(string value, DateTime expiresAt) =>
        new(value, expiresAt);

    public bool IsExpired(DateTime now) => now >= ExpiresAt;
}