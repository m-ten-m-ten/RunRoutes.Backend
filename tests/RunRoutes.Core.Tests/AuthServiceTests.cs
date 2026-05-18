using Microsoft.Extensions.Options;
using Moq;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Settings;
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
        _sut = new AuthService(_userRepoMock.Object, _jwtServiceMock.Object, _emailServiceMock.Object, jwtSettings);
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
            u.ActivationToken != null &&
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
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = EmailAddress.Create("test@example.com"),
            Username = Username.Create("testuser"),
            PasswordHash = HashedPassword.FromHash(BCrypt.Net.BCrypt.HashPassword(password)),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
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
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = EmailAddress.Create("inactive@example.com"),
            IsActive = false,
            PasswordHash = HashedPassword.FromHash(BCrypt.Net.BCrypt.HashPassword("password123"))
        };
        _userRepoMock.Setup(r => r.GetByEmailForUpdateAsync(user.Email.Value)).ReturnsAsync(user);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.LoginAsync(new LoginRequest(user.Email.Value, "password123")));
    }

    [Fact]
    public async Task Login_パスワード不一致でValidationException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = EmailAddress.Create("test@example.com"),
            IsActive = true,
            PasswordHash = HashedPassword.FromHash(BCrypt.Net.BCrypt.HashPassword("correct_password"))
        };
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
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = EmailAddress.Create("test@example.com"),
            Username = Username.Create("testuser"),
            RefreshToken = "old_refresh",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
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
        var user = new User
        {
            RefreshToken = "expired_refresh",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        _userRepoMock.Setup(r => r.GetByRefreshTokenForUpdateAsync("expired_refresh")).ReturnsAsync(user);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.RefreshAsync("expired_refresh"));
    }
}
