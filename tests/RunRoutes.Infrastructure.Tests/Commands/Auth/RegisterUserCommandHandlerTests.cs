using Moq;
using RunRoutes.Core.Auth.Commands.RegisterUser;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Auth;
using RunRoutes.Infrastructure.Commands.Auth;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Auth;

[Collection("Database")]
public class RegisterUserCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private readonly IPasswordHasher _bCryptPasswordHasher = new BCryptPasswordHasher();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private const string TestEmail = "test@example.com";
    private const string TestUserName = "test-name";

    [Fact]
    public async Task HandleAsync_ユーザーを登録できる()
    {
        // Arrange
        await _fixture.ResetAsync();
        _emailServiceMock
            .Setup(e => e.SendActivationEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new UserRepository(db);
            var handler = new RegisterUserCommandHandler(repo, _bCryptPasswordHasher, _emailServiceMock.Object);
            var command = new RegisterUserCommand(TestEmail, TestUserName, "Password123!");
            await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new UserRepository(db);
            Assert.True(await repo.ExistsByEmailAsync(TestEmail));
        }

        _emailServiceMock.Verify(x => x.SendActivationEmailAsync(TestEmail, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_既存メールアドレス登録でConflictException()
    {
        // Arrange
        await _fixture.ResetAsync();

        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated(email: TestEmail);
            db.Users.Add(user);

            await db.SaveChangesAsync();
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new UserRepository(db);
            var handler = new RegisterUserCommandHandler(repo, _bCryptPasswordHasher, _emailServiceMock.Object);
            var command = new RegisterUserCommand(TestEmail, TestUserName, "Password123!");
            var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command, default));
            Assert.Contains("このメールアドレス", ex.Message);
        }
    }

    [Fact]
    public async Task HandleAsync_既存ユーザーネーム登録でConflictException()
    {
        // Arrange
        await _fixture.ResetAsync();

        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated(username: TestUserName);
            db.Users.Add(user);

            await db.SaveChangesAsync();
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new UserRepository(db);
            var handler = new RegisterUserCommandHandler(repo, _bCryptPasswordHasher, _emailServiceMock.Object);
            var command = new RegisterUserCommand(TestEmail, TestUserName, "Password123!");
            var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command, default));
            Assert.Contains("このユーザー名", ex.Message);
        }
    }
}