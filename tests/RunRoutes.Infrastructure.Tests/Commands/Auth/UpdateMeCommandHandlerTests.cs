using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Auth.Commands.UpdateMe;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Auth;
using RunRoutes.Infrastructure.Commands.Auth;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Auth;

[Collection("Database")]
public class UpdateMeCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private readonly IPasswordHasher _bCryptPasswordHasher = new BCryptPasswordHasher();
    private const string TestUserName = "test-name";

    [Fact]
    public async Task HandleAsync_ユーザーネームを更新できる()
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
            var handler = new UpdateMeCommandHandler(repo, _bCryptPasswordHasher);
            var command = new UpdateMeCommand(userId, TestUserName, null, null);
            await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var user = await db.Users.FirstAsync(u => u.Id == userId);
            Assert.Equal(TestUserName, user.Username.Value);
        }
    }

    [Fact]
    public async Task HandleAsync_パスワードを更新できる()
    {
        const string OldPassword = "Password123!";
        const string NewPassword = "newPassword123!";

        // Arrange
        await _fixture.ResetAsync();

        Guid userId;
        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated(password: OldPassword);
            db.Users.Add(user);

            await db.SaveChangesAsync();
            userId = user.Id;
        }

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new UserRepository(db);
            var handler = new UpdateMeCommandHandler(repo, _bCryptPasswordHasher);
            var command = new UpdateMeCommand(userId, null, OldPassword, NewPassword);
            await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var user = await db.Users.FirstAsync(u => u.Id == userId);
            Assert.True(user.VerifyPassword(PlainPassword.CreateForVerification(NewPassword), _bCryptPasswordHasher));
        }
    }

    [Fact]
    public async Task HandleAsync_既存ユーザーネームに更新でConflictException()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid actorId;
        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated(username: TestUserName);
            db.Users.Add(user);

            var actor = TestUserBuilder.CreateActivated();
            db.Users.Add(actor);

            await db.SaveChangesAsync();
            actorId = actor.Id;
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new UserRepository(db);
            var handler = new UpdateMeCommandHandler(repo, _bCryptPasswordHasher);
            var command = new UpdateMeCommand(actorId, TestUserName, null, null);
            var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command, default));
            Assert.Contains("このユーザー名", ex.Message);
        }
    }


    [Fact]
    public async Task HandleAsync_現在のパスワードを未入力でValidationException()
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

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new UserRepository(db);
            var handler = new UpdateMeCommandHandler(repo, _bCryptPasswordHasher);
            var command = new UpdateMeCommand(userId, null, null, "new-password123!");
            var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(command, default));
            Assert.Contains("現在のパスワードを", ex.Message);
        }
    }
}