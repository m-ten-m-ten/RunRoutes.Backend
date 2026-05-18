using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Tests.Common;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Users;

public class UserDomainTests
{
    private readonly IPasswordHasher _hasher = new FakePasswordHasher();

    // ========================================
    // User.Register
    // ========================================

    [Fact]
    public void Register_正常にユーザーが生成される()
    {
        var now = DateTime.UtcNow;
        var email = EmailAddress.Create("test@example.com");
        var username = Username.Create("testuser");
        var password = PlainPassword.Create("password123");

        var user = User.Register(email, username, password, _hasher, now, TimeSpan.FromHours(24));

        Assert.Equal(email, user.Email);
        Assert.Equal(username, user.Username);
        Assert.False(user.IsActive);
        Assert.NotNull(user.Activation);
        Assert.False(user.Activation.IsExpired(now));
        Assert.Equal(now, user.CreatedAt);
        Assert.Equal(now, user.UpdatedAt);
        Assert.Equal(UserRole.User, user.Role);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Register_パスワードがハッシュ化される()
    {
        var password = PlainPassword.Create("password123");
        var user = User.Register(
            EmailAddress.Create("test@example.com"),
            Username.Create("testuser"),
            password,
            _hasher,
            DateTime.UtcNow,
            TimeSpan.FromHours(24));

        Assert.NotEqual(password.Value, user.PasswordHash.Value);
        Assert.True(_hasher.Verify(password, user.PasswordHash));
    }

    [Fact]
    public void Register_複数呼び出しで異なるIDが生成される()
    {
        var now = DateTime.UtcNow;
        var u1 = User.Register(EmailAddress.Create("a@example.com"), Username.Create("usera"), PlainPassword.Create("password123"), _hasher, now, TimeSpan.FromHours(24));
        var u2 = User.Register(EmailAddress.Create("b@example.com"), Username.Create("userb"), PlainPassword.Create("password123"), _hasher, now, TimeSpan.FromHours(24));

        Assert.NotEqual(u1.Id, u2.Id);
    }

    // ========================================
    // User.Activate
    // ========================================

    [Fact]
    public void Activate_正常に有効化される()
    {
        var now = DateTime.UtcNow;
        var user = User.Register(
            EmailAddress.Create("test@example.com"),
            Username.Create("testuser"),
            PlainPassword.Create("password123"),
            _hasher,
            now,
            TimeSpan.FromHours(24));

        user.Activate(now);

        Assert.True(user.IsActive);
        Assert.Null(user.Activation);
        Assert.True(user.UpdatedAt >= now);
    }

    [Fact]
    public void Activate_既に有効化済みでValidationException()
    {
        var now = DateTime.UtcNow;
        var user = User.Register(
            EmailAddress.Create("test@example.com"),
            Username.Create("testuser"),
            PlainPassword.Create("password123"),
            _hasher,
            now,
            TimeSpan.FromHours(24));
        user.Activate(now);

        Assert.Throws<ValidationException>(() => user.Activate(now));
    }

    [Fact]
    public void Activate_期限切れトークンでValidationException()
    {
        var now = DateTime.UtcNow;
        var user = User.Register(
            EmailAddress.Create("test@example.com"),
            Username.Create("testuser"),
            PlainPassword.Create("password123"),
            _hasher,
            now,
            TimeSpan.FromHours(24));

        var expired = now.AddHours(25);
        Assert.Throws<ValidationException>(() => user.Activate(expired));
    }

    // ========================================
    // User.RequestEmailChange
    // ========================================

    [Fact]
    public void RequestEmailChange_正常にEmailChangeが設定される()
    {
        var now = DateTime.UtcNow;
        var user = User.Register(
            EmailAddress.Create("old@example.com"),
            Username.Create("testuser"),
            PlainPassword.Create("password123"),
            _hasher,
            now,
            TimeSpan.FromHours(24));
        user.Activate(now);

        var newEmail = EmailAddress.Create("new@example.com");
        user.RequestEmailChange(newEmail, PlainPassword.Create("password123"), _hasher, now, TimeSpan.FromHours(24));

        Assert.NotNull(user.EmailChange);
        Assert.Equal(newEmail, user.EmailChange.NewEmail);
        Assert.NotEmpty(user.EmailChange.Token);
        Assert.False(user.EmailChange.IsExpired(now));
    }

    [Fact]
    public void RequestEmailChange_パスワード不一致でValidationException()
    {
        var now = DateTime.UtcNow;
        var user = User.Register(
            EmailAddress.Create("old@example.com"),
            Username.Create("testuser"),
            PlainPassword.Create("password123"),
            _hasher,
            now,
            TimeSpan.FromHours(24));
        user.Activate(now);

        Assert.Throws<ValidationException>(() =>
            user.RequestEmailChange(
                EmailAddress.Create("new@example.com"),
                PlainPassword.Create("wrong_password"),
                _hasher,
                now,
                TimeSpan.FromHours(24)));
    }

    // ========================================
    // User.ConfirmEmailChange
    // ========================================

    [Fact]
    public void ConfirmEmailChange_正常にEmailが変更される()
    {
        var now = DateTime.UtcNow;
        var user = User.Register(
            EmailAddress.Create("old@example.com"),
            Username.Create("testuser"),
            PlainPassword.Create("password123"),
            _hasher,
            now,
            TimeSpan.FromHours(24));
        user.Activate(now);

        var newEmail = EmailAddress.Create("new@example.com");
        user.RequestEmailChange(newEmail, PlainPassword.Create("password123"), _hasher, now, TimeSpan.FromHours(24));
        var token = user.EmailChange!.Token;

        user.ConfirmEmailChange(token, now);

        Assert.Equal(newEmail, user.Email);
        Assert.Null(user.EmailChange);
    }

    [Fact]
    public void ConfirmEmailChange_要求なしでValidationException()
    {
        var now = DateTime.UtcNow;
        var user = User.Register(
            EmailAddress.Create("old@example.com"),
            Username.Create("testuser"),
            PlainPassword.Create("password123"),
            _hasher,
            now,
            TimeSpan.FromHours(24));
        user.Activate(now);

        Assert.Throws<ValidationException>(() => user.ConfirmEmailChange("some-token", now));
    }

    [Fact]
    public void ConfirmEmailChange_トークン不一致でValidationException()
    {
        var now = DateTime.UtcNow;
        var user = User.Register(
            EmailAddress.Create("old@example.com"),
            Username.Create("testuser"),
            PlainPassword.Create("password123"),
            _hasher,
            now,
            TimeSpan.FromHours(24));
        user.Activate(now);
        user.RequestEmailChange(EmailAddress.Create("new@example.com"), PlainPassword.Create("password123"), _hasher, now, TimeSpan.FromHours(24));

        Assert.Throws<ValidationException>(() => user.ConfirmEmailChange("wrong-token", now));
    }

    [Fact]
    public void ConfirmEmailChange_期限切れトークンでValidationException()
    {
        var now = DateTime.UtcNow;
        var user = User.Register(
            EmailAddress.Create("old@example.com"),
            Username.Create("testuser"),
            PlainPassword.Create("password123"),
            _hasher,
            now,
            TimeSpan.FromHours(24));
        user.Activate(now);
        user.RequestEmailChange(EmailAddress.Create("new@example.com"), PlainPassword.Create("password123"), _hasher, now, TimeSpan.FromHours(24));
        var token = user.EmailChange!.Token;

        var expired = now.AddHours(25);
        Assert.Throws<ValidationException>(() => user.ConfirmEmailChange(token, expired));
    }
}
