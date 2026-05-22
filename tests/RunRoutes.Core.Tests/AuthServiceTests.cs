using Microsoft.Extensions.Options;
using Moq;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Sessions;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Tests.Common;
using RunRoutes.Core.Users;
using RunRoutes.Core.Users.Dtos;
using RunRoutes.Core.Users.Events;

namespace RunRoutes.Core.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ISessionRepository> _sessionRepoMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "test-secret-key",
            Issuer = "test",
            Audience = "test",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });
        _sut = new AuthService(_userRepoMock.Object, _sessionRepoMock.Object, _jwtServiceMock.Object, _emailServiceMock.Object, new FakePasswordHasher(), jwtSettings);
    }

    [Fact]
    public async Task Register_正常に登録できる()
    {
        var request = new RegisterRequest("test@example.com", "testuser", "password123");
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.ExistsByUsernameAsync(request.Username)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _emailServiceMock
            .Setup(e => e.SendActivationEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _sut.RegisterAsync(request);

        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email.Value == request.Email &&
            u.Username.Value == request.Username &&
            u.PasswordHash.Value != request.Password &&
            u.Activation != null &&
            !u.IsActive
        )), Times.Once);
    }

    [Fact]
    public async Task Register_メール重複でConflictException()
    {
        var request = new RegisterRequest("dup@example.com", "dupuser", "password123");
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.RegisterAsync(request));
    }

    [Fact]
    public async Task Register_ユーザー名重複でConflictException()
    {
        var request = new RegisterRequest("test@example.com", "dupuser", "password123");
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.ExistsByUsernameAsync(request.Username)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.RegisterAsync(request));
    }

    [Fact]
    public async Task Login_正常にトークンが発行される()
    {
        var password = "password123";
        var user = UserTestFactory.CreateActivated("test@example.com", "testuser", password);
        var request = new LoginRequest(user.Email.Value, password);

        Session? captured = null;
        _userRepoMock.Setup(r => r.GetByEmailForUpdateAsync(request.Email)).ReturnsAsync(user);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user)).Returns("access_token");
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<Session>()))
            .Callback<Session>(s => captured = s)
            .Returns(Task.CompletedTask);

        var (response, refreshToken) = await _sut.LoginAsync(request);

        Assert.Equal("access_token", response.AccessToken);
        Assert.Equal(user.Id, response.User.Id);
        Assert.NotEmpty(refreshToken);
        Assert.NotNull(captured);
        Assert.Equal(user.Id, captured!.UserId);
        Assert.Equal(refreshToken, captured.RefreshToken.Value);
        _sessionRepoMock.Verify(r => r.AddAsync(It.IsAny<Session>()), Times.Once);
    }

    [Fact]
    public async Task Login_未有効化でValidationException()
    {
        var user = UserTestFactory.CreateInactive("inactive@example.com", "inactiveuser");
        _userRepoMock.Setup(r => r.GetByEmailForUpdateAsync(user.Email.Value)).ReturnsAsync(user);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.LoginAsync(new LoginRequest(user.Email.Value, "password123")));
    }

    [Fact]
    public async Task Login_パスワード不一致でValidationException()
    {
        var user = UserTestFactory.CreateActivated("test@example.com", "testuser", "correct_password");
        _userRepoMock.Setup(r => r.GetByEmailForUpdateAsync(user.Email.Value)).ReturnsAsync(user);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.LoginAsync(new LoginRequest(user.Email.Value, "wrong_password")));
    }

    [Fact]
    public async Task Login_8文字未満の既存パスワードでもログインできる()
    {
        // 8文字ポリシー導入前に作られた短いパスワードの既存ユーザーを再現する
        var now = DateTime.UtcNow;
        var user = User.Register(
            EmailAddress.Create("legacy@example.com"),
            Username.Create("legacyuser"),
            PlainPassword.CreateForVerification("pass"),
            new FakePasswordHasher(),
            now,
            TimeSpan.FromHours(24));
        user.Activate(now);

        _userRepoMock.Setup(r => r.GetByEmailForUpdateAsync(user.Email.Value)).ReturnsAsync(user);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user)).Returns("access_token");
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<Session>())).Returns(Task.CompletedTask);

        var (response, _) = await _sut.LoginAsync(new LoginRequest(user.Email.Value, "pass"));

        Assert.Equal("access_token", response.AccessToken);
    }

    [Fact]
    public async Task Login_存在しないメールでValidationException()
    {
        _userRepoMock
            .Setup(r => r.GetByEmailForUpdateAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.LoginAsync(new LoginRequest("noexist@example.com", "password123")));
    }

    [Fact]
    public async Task Logout_正常にセッションが削除される()
    {
        var token = RefreshToken.Generate(DateTime.UtcNow, TimeSpan.FromDays(7));
        var session = Session.Start(Guid.NewGuid(), token, DateTime.UtcNow);

        _sessionRepoMock.Setup(r => r.GetByRefreshTokenForUpdateAsync(token.Value)).ReturnsAsync(session);
        _sessionRepoMock.Setup(r => r.DeleteAsync(session)).Returns(Task.CompletedTask);

        await _sut.LogoutAsync(token.Value);

        _sessionRepoMock.Verify(r => r.DeleteAsync(session), Times.Once);
    }

    [Fact]
    public async Task Logout_セッションが存在しなければ何もしない()
    {
        _sessionRepoMock.Setup(r => r.GetByRefreshTokenForUpdateAsync(It.IsAny<string>()))
            .ReturnsAsync((Session?)null);

        await _sut.LogoutAsync("unknown_refresh");

        _sessionRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_正常に新トークンが発行される()
    {
        var now = DateTime.UtcNow;
        var user = UserTestFactory.CreateActivated("test@example.com", "testuser");
        var oldToken = RefreshToken.Generate(now, TimeSpan.FromDays(7));
        var session = Session.Start(user.Id, oldToken, now);

        _sessionRepoMock.Setup(r => r.GetByRefreshTokenForUpdateAsync(oldToken.Value)).ReturnsAsync(session);
        _userRepoMock.Setup(r => r.GetByIdAsync(session.UserId)).ReturnsAsync(user);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user)).Returns("new_access_token");
        _sessionRepoMock.Setup(r => r.UpdateAsync(session)).Returns(Task.CompletedTask);

        var (response, newRefreshToken) = await _sut.RefreshAsync(oldToken.Value);

        Assert.Equal("new_access_token", response.AccessToken);
        Assert.NotEmpty(newRefreshToken);
        Assert.NotEqual(oldToken.Value, newRefreshToken);
        Assert.Equal(newRefreshToken, session.RefreshToken.Value);
        _sessionRepoMock.Verify(r => r.UpdateAsync(session), Times.Once);
    }

    [Fact]
    public async Task Refresh_無効なトークンでValidationException()
    {
        _sessionRepoMock.Setup(r => r.GetByRefreshTokenForUpdateAsync(It.IsAny<string>()))
            .ReturnsAsync((Session?)null);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.RefreshAsync("invalid_refresh"));
    }

    [Fact]
    public async Task Refresh_期限切れトークンでValidationException()
    {
        var expiredToken = RefreshToken.FromStorage("expired_refresh", DateTime.UtcNow.AddDays(-1));
        var session = Session.Start(Guid.NewGuid(), expiredToken, DateTime.UtcNow.AddDays(-8));

        _sessionRepoMock.Setup(r => r.GetByRefreshTokenForUpdateAsync("expired_refresh")).ReturnsAsync(session);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.RefreshAsync("expired_refresh"));
    }

    [Fact]
    public async Task UpdateEmail_正常にメール変更要求が送信される()
    {
        var user = UserTestFactory.CreateActivated("old@example.com", "testuser", "password123");
        var request = new UpdateEmailRequest("new@example.com", "password123");

        _userRepoMock.Setup(r => r.GetByIdForUpdateAsync(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("new@example.com")).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);
        _emailServiceMock
            .Setup(e => e.SendEmailChangeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _sut.UpdateEmailAsync(user.Id, request);

        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.EmailChange != null &&
            u.EmailChange.NewEmail.Value == "new@example.com"
        )), Times.Once);
        _emailServiceMock.Verify(e => e.SendEmailChangeEmailAsync("new@example.com", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEmail_パスワード不一致でValidationException()
    {
        var user = UserTestFactory.CreateActivated("old@example.com", "testuser", "correct_password");
        var request = new UpdateEmailRequest("new@example.com", "wrong_password");

        _userRepoMock.Setup(r => r.GetByIdForUpdateAsync(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("new@example.com")).ReturnsAsync(false);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.UpdateEmailAsync(user.Id, request));
    }

    [Fact]
    public async Task UpdateEmail_メール重複でConflictException()
    {
        var user = UserTestFactory.CreateActivated("old@example.com", "testuser", "password123");
        var request = new UpdateEmailRequest("dup@example.com", "password123");

        _userRepoMock.Setup(r => r.GetByIdForUpdateAsync(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("dup@example.com")).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.UpdateEmailAsync(user.Id, request));
    }

    [Fact]
    public async Task ActivateEmail_正常にメールが変更される()
    {
        var now = DateTime.UtcNow;
        var user = UserTestFactory.CreateActivated("old@example.com", "testuser", "password123");
        user.RequestEmailChange(
            EmailAddress.Create("new@example.com"),
            PlainPassword.Create("password123"),
            new FakePasswordHasher(),
            now,
            TimeSpan.FromHours(24));
        var token = user.EmailChange!.Token;

        _userRepoMock.Setup(r => r.GetByEmailChangeTokenForUpdateAsync(token)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

        await _sut.ActivateEmailAsync(token);

        Assert.Equal("new@example.com", user.Email.Value);
        Assert.Null(user.EmailChange);
    }

    [Fact]
    public async Task UpdateMe_ユーザー名を正常に変更できる()
    {
        var user = UserTestFactory.CreateActivated("test@example.com", "olduser", "password123");
        var request = new UpdateMeRequest("newuser", null, null);

        _userRepoMock.Setup(r => r.GetByIdForUpdateAsync(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.ExistsByUsernameAsync("newuser")).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

        var response = await _sut.UpdateMeAsync(user.Id, request);

        Assert.Equal("newuser", response.User.Username);
        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.Username.Value == "newuser")), Times.Once);
    }

    [Fact]
    public async Task UpdateMe_ユーザー名重複でConflictException()
    {
        var user = UserTestFactory.CreateActivated("test@example.com", "olduser", "password123");
        var request = new UpdateMeRequest("dupuser", null, null);

        _userRepoMock.Setup(r => r.GetByIdForUpdateAsync(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.ExistsByUsernameAsync("dupuser")).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.UpdateMeAsync(user.Id, request));
    }

    [Fact]
    public async Task UpdateMe_パスワードを正常に変更できる()
    {
        var user = UserTestFactory.CreateActivated("test@example.com", "testuser", "old_password");
        var request = new UpdateMeRequest(null, "old_password", "new_password");

        _userRepoMock.Setup(r => r.GetByIdForUpdateAsync(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

        await _sut.UpdateMeAsync(user.Id, request);

        Assert.True(user.VerifyPassword(PlainPassword.Create("new_password"), new FakePasswordHasher()));
    }

    [Fact]
    public async Task UpdateMe_新パスワードありで現在のパスワード未入力でValidationException()
    {
        var user = UserTestFactory.CreateActivated("test@example.com", "testuser", "old_password");
        var request = new UpdateMeRequest(null, null, "new_password");

        _userRepoMock.Setup(r => r.GetByIdForUpdateAsync(user.Id)).ReturnsAsync(user);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.UpdateMeAsync(user.Id, request));
    }

    [Fact]
    public async Task RemoveMe_正常に削除されDeleteAccountResponseが返る()
    {
        var user = UserTestFactory.CreateActivated("delete@example.com", "deleteuser");
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.RemoveAsync(user)).Returns(Task.CompletedTask);

        var response = await _sut.RemoveMeAsync(user.Id);

        Assert.Equal("アカウントを削除しました", response.Message);
        _userRepoMock.Verify(r => r.RemoveAsync(It.Is<User>(u =>
            u.Id == user.Id &&
            u.DomainEvents.OfType<UserRemovedEvent>().Any(e => e.UserId == user.Id)
        )), Times.Once);
    }

    [Fact]
    public async Task RemoveMe_存在しないユーザーでNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RemoveMeAsync(userId));

        _userRepoMock.Verify(r => r.RemoveAsync(It.IsAny<User>()), Times.Never);
    }
}
