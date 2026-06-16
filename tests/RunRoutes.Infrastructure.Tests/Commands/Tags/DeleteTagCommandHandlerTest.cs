using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Tags.Commands.DeleteTag;
using RunRoutes.Core.Tags.Commands.UpdateTag;
using RunRoutes.Infrastructure.Commands.Tags;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Tags;

[Collection("Database")]
public class DeleteTagCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_タグを削除できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        Tag tag;
        await using (var db = _fixture.CreateDbContext())
        {
            tag = Tag.Create("タグネーム");
            db.Tags.Add(tag);
            await db.SaveChangesAsync();
        }

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new TagRepository(db);
            var handler = new DeleteTagCommandHandler(repo);
            var command = new DeleteTagCommand(tag.Id, tag.Version);
            await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var deletedTag = await db.Tags.FirstOrDefaultAsync(t => t.Id == tag.Id);
            Assert.Null(deletedTag);
        }
    }

    [Fact]
    public async Task HandleAsync_使用中でConflictException()
    {
        // Arrange
        await _fixture.ResetAsync();

        Tag tag;
        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated();
            db.Users.Add(user);

            tag = Tag.Create("タグネーム");
            db.Tags.Add(tag);

            await db.SaveChangesAsync();

            var course = Course.Create(
            userId: user.Id,
            title: "テストコース",
            description: null,
            difficulty: Difficulty.Easy,
            route: new LineString([new Coordinate(141.3507, 43.0686), new Coordinate(141.3522, 43.0700)]),
            isPublic: true,
            tags: [tag]);
            db.Courses.Add(course);
            await db.SaveChangesAsync();
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new TagRepository(db);
            var handler = new DeleteTagCommandHandler(repo);
            var command = new DeleteTagCommand(tag.Id, tag.Version);
            await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command, default));
        }
    }

    [Fact]
    public async Task HandleAsync_楽観ロック衝突でConflictException()
    {
        // Arrange
        await _fixture.ResetAsync();

        Tag tag;
        uint v1;
        await using (var db = _fixture.CreateDbContext())
        {
            tag = Tag.Create("タグネーム");
            db.Tags.Add(tag);
            await db.SaveChangesAsync();
            v1 = tag.Version;
        }

        await using (var db = _fixture.CreateDbContext())
        {
            tag = await db.Tags.SingleAsync(t => t.Id == tag.Id);
            tag.Rename("タグネーム2");
            await db.SaveChangesAsync();
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new TagRepository(db);
            var handler = new DeleteTagCommandHandler(repo);
            var command = new DeleteTagCommand(tag.Id, v1);
            await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command, default));
        }
    }

}