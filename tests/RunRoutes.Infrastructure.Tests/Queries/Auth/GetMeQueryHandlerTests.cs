using RunRoutes.Core.Auth.Queries.GetMe;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;
using RunRoutes.Core.Users.Dtos;
using RunRoutes.Infrastructure.Queries.Auth;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Queries.Auth;

[Collection("Database")]
public class GetMeQueryHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_自分を取得できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        User user;
        await using (var db = _fixture.CreateDbContext())
        {
            user = TestUserBuilder.CreateActivated();
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Act
        MeResponse response;
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new GetMeQueryHandler(db);
            response = await handler.HandleAsync(new GetMeQuery(user.Id), default);
        }

        // Assert
        Assert.Equal(user.Id, response.User.Id);
        Assert.Equal(user.Email.Value, response.User.Email);
        Assert.Equal(user.Username.Value, response.User.Username);
        Assert.Equal(user.Role.ToString(), response.User.Role);
    }

    [Fact]
    public async Task HandleAsync_存在しないユーザーでNotFoundException()
    {
        // Arrange
        await _fixture.ResetAsync();

        // Act & Assert
        await using var db = _fixture.CreateDbContext();
        var handler = new GetMeQueryHandler(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
        handler.HandleAsync(new GetMeQuery(Guid.NewGuid()), default));
    }
}

