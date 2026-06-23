using Microsoft.Extensions.Options;
using Moq;
using RunRoutes.Core.Auth.Commands.Login;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Auth;
using RunRoutes.Infrastructure.Commands.Auth;
using RunRoutes.Infrastructure.Data;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Auth;

[Collection("Database")]
public class LoginCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private readonly IPasswordHasher _bCryptPasswordHasher = new BCryptPasswordHasher();
    private const string TestPassword = "Password123!";
    private const string TestAccessToken = "dummy-access-token";

    [Fact]
    public async Task HandleAsync_正常にログインできる()
    {
        // Arrange
        await _fixture.ResetAsync();

        User user;
        await using (var db = _fixture.CreateDbContext())
        {
            user = TestUserBuilder.CreateActivated(password: TestPassword);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Act
        LoginResult result;
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = CreateHandler(db);
            var command = new LoginCommand(user.Email.Value, TestPassword);
            result = await handler.HandleAsync(command, default);
        }

        // Assert
        Assert.Equal(TestAccessToken, result.Response.AccessToken);
        Assert.Equal(user.Id, result.Response.User.Id);
        Assert.NotEmpty(result.RefreshToken);
        await using (var db = _fixture.CreateDbContext())
        {
            var sessionRepo = new SessionRepository(db);
            var session = await sessionRepo.GetByRefreshTokenForUpdateAsync(result.RefreshToken);
            Assert.NotNull(session);
        }
    }

    [Fact]
    public async Task HandleAsync_未有効化でValidationException()
    {
        // Arrange
        await _fixture.ResetAsync();

        User user;
        await using (var db = _fixture.CreateDbContext())
        {
            user = TestUserBuilder.CreateUnActivated(password: TestPassword);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = CreateHandler(db);
            var command = new LoginCommand(user.Email.Value, TestPassword);
            await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(command, default));
        }
    }

    [Fact]
    public async Task HandleAsync_パスワード不一致でValidationException()
    {
        // Arrange
        await _fixture.ResetAsync();

        User user;
        await using (var db = _fixture.CreateDbContext())
        {
            user = TestUserBuilder.CreateActivated(password: TestPassword);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = CreateHandler(db);
            var command = new LoginCommand(user.Email.Value, "WrongPassword");
            await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(command, default));
        }
    }

    private LoginCommandHandler CreateHandler(AppDbContext db)
    {
        var jwtServiceMock = new Mock<IJwtService>();
        jwtServiceMock.Setup(s => s.GenerateAccessToken(It.IsAny<User>()))
            .Returns(TestAccessToken);

        var jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "test-secret-key",
            Issuer = "test",
            Audience = "test",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });

        return new LoginCommandHandler(
            new UserRepository(db),
            new SessionRepository(db),
            jwtServiceMock.Object,
            _bCryptPasswordHasher,
            jwtSettings);
    }
}
