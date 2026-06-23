using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Auth.Commands.RemoveMe;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Infrastructure.Commands.Auth;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Auth;

[Collection("Database")]
public class RemoveMeCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_ユーザーを削除できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid userId;
        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated();
            db.Users.Add(user);

            await db.SaveChangesAsync();
            userId = user.Id;
        }

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new UserRepository(db);
            var handler = new RemoveMeCommandHandler(repo);
            var command = new RemoveMeCommand(userId);
            await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            Assert.Null(user);
        }
    }

    [Fact]
    public async Task HandleAsync_存在しないユーザー削除でNotFoundException()
    {
        // Arrange
        await _fixture.ResetAsync();

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new UserRepository(db);
            var handler = new RemoveMeCommandHandler(repo);
            var command = new RemoveMeCommand(Guid.NewGuid());
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command, default));
            Assert.Contains("ユーザーが見つかりません", ex.Message);
        }
    }
}