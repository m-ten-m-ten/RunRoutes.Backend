using Microsoft.EntityFrameworkCore;
using Moq;
using RunRoutes.Core.Auth.Commands.UpdateEmail;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Auth;
using RunRoutes.Infrastructure.Commands.Auth;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Auth;

[Collection("Database")]
public class UpdateEmailCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private readonly IPasswordHasher _bCryptPasswordHasher = new BCryptPasswordHasher();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private const string TestEmail = "test@example.com";
    private const string TestPassword = "Password123!";

    [Fact]
    public async Task HandleAsync_メールアドレスを変更できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid userId;
        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated(password: TestPassword);
            db.Users.Add(user);

            await db.SaveChangesAsync();
            userId = user.Id;
        }

        _emailServiceMock
            .Setup(e => e.SendEmailChangeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new UserRepository(db);
            var handler = new UpdateEmailCommandHandler(repo, _bCryptPasswordHasher, _emailServiceMock.Object);
            var command = new UpdateEmailCommand(userId, TestEmail, TestPassword);
            await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new UserRepository(db);
            var user = await db.Users.FirstAsync(u => u.Id == userId);
            Assert.NotNull(user.EmailChange);
            Assert.Equal(TestEmail, user.EmailChange.NewEmail.Value);
        }

        _emailServiceMock.Verify(x => x.SendEmailChangeEmailAsync(TestEmail, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_既存メールアドレス登録でConflictException()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid actorId;
        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated(email: TestEmail);
            db.Users.Add(user);

            var actor = TestUserBuilder.CreateActivated(password: TestPassword);
            db.Users.Add(actor);

            await db.SaveChangesAsync();
            actorId = actor.Id;
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new UserRepository(db);
            var handler = new UpdateEmailCommandHandler(repo, _bCryptPasswordHasher, _emailServiceMock.Object);
            var command = new UpdateEmailCommand(actorId, TestEmail, TestPassword);
            var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command, default));
            Assert.Contains("このメールアドレス", ex.Message);
        }
    }
}