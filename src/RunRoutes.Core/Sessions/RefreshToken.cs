using System.Security.Cryptography;
using RunRoutes.Core.Common.Exceptions;

namespace RunRoutes.Core.Sessions;

public sealed record RefreshToken
{
    public string Value { get; }
    public DateTime ExpiresAt { get; }

    private RefreshToken(string value, DateTime expiresAt)
    {
        Value = value;
        ExpiresAt = expiresAt;
    }

    public static RefreshToken Generate(DateTime now, TimeSpan validity)
    {
        if (validity <= TimeSpan.Zero)
            throw new ValidationException("有効期間は正の値である必要があります");

        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new RefreshToken(value, now + validity);
    }

    public static RefreshToken FromStorage(string value, DateTime expiresAt) =>
        new(value, expiresAt);

    public bool IsExpired(DateTime now) => now >= ExpiresAt;
}