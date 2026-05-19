using Microsoft.Extensions.Options;
using Moq;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Tests.Common;
using RunRoutes.Core.Users;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
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
        _sut = new AuthService(_userRepoMock.Object, _jwtServiceMock.Object, _emailServiceMock.Object, new FakePasswordHasher(), jwtSettings);
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

        _userRepoMock.Setup(r => r.GetByEmailForUpdateAsync(request.Email)).ReturnsAsync(user);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user)).Returns("access_token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh_token");
        _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

        var (response, refreshToken) = await _sut.LoginAsync(request);

        Assert.Equal("access_token", response.AccessToken);
        Assert.Equal(user.Id, response.User.Id);
        Assert.Equal("refresh_token", refreshToken);
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
    public async Task Login_存在しないメールでValidationException()
    {
        _userRepoMock
            .Setup(r => r.GetByEmailForUpdateAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.LoginAsync(new LoginRequest("noexist@example.com", "password123")));
    }

    [Fact]
    public async Task Refresh_正常に新トークンが発行される()
    {
        var user = UserTestFactory.CreateActivated("test@example.com", "testuser");
        user.RefreshToken = "old_refresh";
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        _userRepoMock.Setup(r => r.GetByRefreshTokenForUpdateAsync("old_refresh")).ReturnsAsync(user);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(user)).Returns("new_access_token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("new_refresh_token");
        _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

        var (response, newRefreshToken) = await _sut.RefreshAsync("old_refresh");

        Assert.Equal("new_access_token", response.AccessToken);
        Assert.Equal("new_refresh_token", newRefreshToken);
    }

    [Fact]
    public async Task Refresh_期限切れトークンでValidationException()
    {
        var user = UserTestFactory.CreateActivated();
        user.RefreshToken = "expired_refresh";
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1);

        _userRepoMock.Setup(r => r.GetByRefreshTokenForUpdateAsync("expired_refresh")).ReturnsAsync(user);

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
}
