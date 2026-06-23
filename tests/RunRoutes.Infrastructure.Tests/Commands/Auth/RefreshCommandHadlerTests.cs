using Microsoft.Extensions.Options;
using Moq;
using RunRoutes.Core.Auth.Commands.Refresh;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Sessions;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Commands.Auth;
using RunRoutes.Infrastructure.Data;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Auth;

[Collection("Database")]
public class RefreshCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private const string TestPassword = "Password123!";
    private const string TestAccessToken = "dummy-access-token";

    [Fact]
    public async Task HandleAsync_正常にリフレッシュできる()
    {
        // Arrange
        await _fixture.ResetAsync();

        User user;
        string oldTokenStr;
        await using (var db = _fixture.CreateDbContext())
        {
            user = TestUserBuilder.CreateActivated(password: TestPassword);
            db.Users.Add(user);
            var now = DateTime.UtcNow;
            var oldToken = RefreshToken.Generate(now, TimeSpan.FromDays(7));
            var session = Session.Start(user.Id, oldToken, now);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
            oldTokenStr = oldToken.Value;
        }

        // Act
        RefreshResult result;
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = CreateHandler(db);
            var command = new RefreshCommand(oldTokenStr);
            result = await handler.HandleAsync(command, default);
        }

        // Assert
        Assert.Equal(TestAccessToken, result.Response.AccessToken);
        Assert.Equal(user.Id, result.Response.User.Id);
        Assert.NotEmpty(result.NewRefreshToken);
        Assert.NotEqual(oldTokenStr, result.NewRefreshToken);
        await using (var db = _fixture.CreateDbContext())
        {
            var sessionRepo = new SessionRepository(db);
            var session = await sessionRepo.GetByRefreshTokenForUpdateAsync(oldTokenStr);
            Assert.Null(session);
            session = await sessionRepo.GetByRefreshTokenForUpdateAsync(result.NewRefreshToken);
            Assert.NotNull(session);
        }
    }

    [Fact]
    public async Task HandleAsync_期限切れSessionでValidationException()
    {
        // Arrange
        await _fixture.ResetAsync();

        User user;
        string expiredTokenStr;
        await using (var db = _fixture.CreateDbContext())
        {
            user = TestUserBuilder.CreateActivated(password: TestPassword);
            db.Users.Add(user);
            var tenDaysAgo = DateTime.UtcNow.AddDays(-10);
            var expiredToken = RefreshToken.Generate(tenDaysAgo, TimeSpan.FromDays(7));
            var session = Session.Start(user.Id, expiredToken, tenDaysAgo);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
            expiredTokenStr = expiredToken.Value;
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = CreateHandler(db);
            var command = new RefreshCommand(expiredTokenStr);
            await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(command, default));
        }
    }
    [Fact]
    public async Task HandleAsync_存在しないrefreshTokenでValidationException()
    {
        // Arrange
        await _fixture.ResetAsync();

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = CreateHandler(db);
            var command = new RefreshCommand("nonexistent-token");
            await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(command, default));
        }
    }

    private RefreshCommandHandler CreateHandler(AppDbContext db)
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

        return new RefreshCommandHandler(
            new UserRepository(db),
            new SessionRepository(db),
            jwtServiceMock.Object,
            jwtSettings);
    }


}
