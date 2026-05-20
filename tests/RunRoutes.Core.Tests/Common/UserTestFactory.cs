using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Common;

public static class UserTestFactory
{
    public static User CreateActivated(
        string email = "test@example.com",
        string username = "testuser",
        string password = "password123",
        IPasswordHasher? hasher = null)
    {
        hasher ??= new FakePasswordHasher();
        var now = DateTime.UtcNow;
        var user = User.Register(
            EmailAddress.Create(email),
            Username.Create(username),
            PlainPassword.Create(password),
            hasher,
            now,
            TimeSpan.FromHours(24));
        user.Activate(now);
        return user;
    }

    public static User CreateInactive(
        string email = "test@example.com",
        string username = "testuser",
        string password = "password123",
        IPasswordHasher? hasher = null)
    {
        hasher ??= new FakePasswordHasher();
        var now = DateTime.UtcNow;
        return User.Register(
            EmailAddress.Create(email),
            Username.Create(username),
            PlainPassword.Create(password),
            hasher,
            now,
            TimeSpan.FromHours(24));
    }
}
