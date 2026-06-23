using RunRoutes.Core.Auth.Commands.Logout;
using RunRoutes.Core.Sessions;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Auth;
using RunRoutes.Infrastructure.Commands.Auth;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Auth;

[Collection("Database")]
public class LogoutCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_正常にログアウトできる()
    {
        // Arrange
        await _fixture.ResetAsync();

        string tokenStr;
        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated();
            db.Users.Add(user);
            var now = DateTime.UtcNow;
            var token = RefreshToken.Generate(now, TimeSpan.FromDays(7));
            var session = Session.Start(user.Id, token, now);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            tokenStr = token.Value;
        }

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new LogoutCommandHandler(new SessionRepository(db));
            var command = new LogoutCommand(tokenStr);
            await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var sessionRepo = new SessionRepository(db);
            var session = await sessionRepo.GetByRefreshTokenForUpdateAsync(tokenStr);
            Assert.Null(session);
        }
    }
    [Fact]
    public async Task HandleAsync_セッション不在で正常終了()
    {
        // Arrange
        await _fixture.ResetAsync();

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new LogoutCommandHandler(new SessionRepository(db));
            var command = new LogoutCommand("wrong-token");
            await handler.HandleAsync(command, default);
            // 例外が発生せず、完走すればOK
        }
    }

}
