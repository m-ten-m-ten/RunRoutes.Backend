using RunRoutes.Core.Users;

namespace RunRoutes.Infrastructure.Tests;

internal static class TestUserBuilder
{
    private static readonly IPasswordHasher BcryptHasher = new BcryptTestHasher();

    /// <summary>
    /// 有効化済みの一般ユーザーを作成する（コース所有者などのデフォルト用途）
    /// </summary>
    public static User CreateActivated(
        string? email = null,
        string? username = null,
        string password = "Password123!",
        UserRole role = UserRole.User
    )
    {
        var now = DateTime.UtcNow;

        // メール・ユーザー名は一意性が必要なので、未指定なら自動生成
        email ??= $"test{Guid.NewGuid():N}@example.com";
        username ??= $"user_{Guid.NewGuid():N}"[..16];

        var user = User.Register(
            EmailAddress.Create(email),
            Username.Create(username),
            PlainPassword.Create(password),
            BcryptHasher,
            now,
            TimeSpan.FromHours(24)
        );

        user.Activate(now);

        if (role == UserRole.Admin) user.PromoteToAdmin(now);

        return user;
    }

    /// <summary>
    /// 未有効化の一般ユーザーを作成する
    /// </summary>
    public static User CreateUnActivated(
    string? email = null,
    string? username = null,
    string password = "Password123!")
    {
        var now = DateTime.UtcNow;
        email ??= $"test{Guid.NewGuid():N}@example.com";
        username ??= $"user_{Guid.NewGuid():N}"[..16];

        return User.Register(
            EmailAddress.Create(email),
            Username.Create(username),
            PlainPassword.Create(password),
            BcryptHasher,
            now,
            TimeSpan.FromHours(24));
        // Activate を呼ばない
    }

    private sealed class BcryptTestHasher : IPasswordHasher
    {
        public HashedPassword Hash(PlainPassword plain) =>
            HashedPassword.FromHash(BCrypt.Net.BCrypt.HashPassword(plain.Value));

        public bool Verify(PlainPassword plain, HashedPassword hashed) =>
            BCrypt.Net.BCrypt.Verify(plain.Value, hashed.Value);
    }
}