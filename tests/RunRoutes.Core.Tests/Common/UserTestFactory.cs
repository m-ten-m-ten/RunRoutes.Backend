using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Common;

public static class UserTestFactory
{
    public static User CreateActivated(
        string email = "test@example.com",
        string username = "testuser",
        string password = "password123",
        IPasswordHasher? hasher = null,
        UserRole role = UserRole.User)
    {
        hasher ??= new FakePasswordHasher();
        var now = DateTime.UtcNow;
        return new User
        {
            Id = Guid.NewGuid(),
            Email = EmailAddress.Create(email),
            Username = Username.Create(username),
            PasswordHash = hasher.Hash(PlainPassword.Create(password)),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Role = role,
        };
    }

    public static User CreateInactive(
        string email = "test@example.com",
        string username = "testuser",
        string password = "password123",
        IPasswordHasher? hasher = null)
    {
        var user = CreateActivated(email, username, password, hasher);
        user.IsActive = false;
        return user;
    }
}
