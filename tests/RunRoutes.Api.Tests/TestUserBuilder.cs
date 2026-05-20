using RunRoutes.Core.Users;

namespace RunRoutes.Api.Tests;

internal static class TestUserBuilder
{
    private static readonly IPasswordHasher BcryptHasher = new BcryptTestHasher();

    public static User CreateActivated(string email, string username, string password, UserRole role = UserRole.User)
    {
        var now = DateTime.UtcNow;
        var user = User.Register(
            EmailAddress.Create(email),
            Username.Create(username),
            PlainPassword.Create(password),
            BcryptHasher,
            now,
            TimeSpan.FromHours(24));
        user.Activate(now);
        if (role == UserRole.Admin)
            user.PromoteToAdmin(now);
        return user;
    }

    private sealed class BcryptTestHasher : IPasswordHasher
    {
        public HashedPassword Hash(PlainPassword plain) =>
            HashedPassword.FromHash(BCrypt.Net.BCrypt.HashPassword(plain.Value));

        public bool Verify(PlainPassword plain, HashedPassword hashed) =>
            BCrypt.Net.BCrypt.Verify(plain.Value, hashed.Value);
    }
}
