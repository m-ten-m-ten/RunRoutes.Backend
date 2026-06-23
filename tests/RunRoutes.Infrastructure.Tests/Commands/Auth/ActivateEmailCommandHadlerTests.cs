using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Auth.Commands.ActivateEmail;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Auth;
using RunRoutes.Infrastructure.Commands.Auth;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Auth;

[Collection("Database")]
public class ActivateEmailCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private readonly IPasswordHasher _bCryptPasswordHasher = new BCryptPasswordHasher();
    private const string TestPassword = "Password123!";

    [Fact]
    public async Task HandleAsync_正常にアクティベートできる()
    {
        // Arrange
        await _fixture.ResetAsync();

        User user;
        const string oldEmail = "old@example.com";
        const string newEmail = "new@example.com";
        await using (var db = _fixture.CreateDbContext())
        {
            user = TestUserBuilder.CreateUnActivated(email: oldEmail, password: TestPassword);
            user.RequestEmailChange(
                EmailAddress.Create(newEmail),
                PlainPassword.Create(TestPassword),
                _bCryptPasswordHasher,
                DateTime.UtcNow,
                TimeSpan.FromMinutes(10));
            db.Users.Add(user);

            await db.SaveChangesAsync();
        }

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new ActivateEmailCommandHandler(new UserRepository(db));
            var command = new ActivateEmailCommand(user.EmailChange!.Token);
            await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var userForAssert = await db.Users.FirstAsync(u => u.Id == user.Id);
            Assert.Equal(newEmail, userForAssert.Email.Value);
            Assert.Null(userForAssert.EmailChange);
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
            var handler = new ActivateEmailCommandHandler(new UserRepository(db));
            var command = new ActivateEmailCommand("wrong-token");
            await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command, default));
        }
    }
}
