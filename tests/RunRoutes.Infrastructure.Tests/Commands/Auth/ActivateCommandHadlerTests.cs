using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Auth.Commands.Activate;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Commands.Auth;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Auth;

[Collection("Database")]
public class ActivateCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private const string TestPassword = "Password123!";

    [Fact]
    public async Task HandleAsync_正常にアクティベートできる()
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

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new ActivateCommandHandler(new UserRepository(db));
            var command = new ActivateCommand(user.Activation!.Value);
            await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var userForAssert = await db.Users.FirstAsync(u => u.Id == user.Id);
            Assert.True(userForAssert.IsActive);
            Assert.Null(userForAssert.Activation);
        }
    }

    [Fact]
    public async Task HandleAsync_無効トークンでNotFoundException()
    {
        // Arrange
        await _fixture.ResetAsync();

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new ActivateCommandHandler(new UserRepository(db));
            var command = new ActivateCommand("wrong-token");
            await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command, default));
        }
    }
}
