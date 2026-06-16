using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Tags.Commands.UpdateTag;
using RunRoutes.Core.Tags.Dtos;
using RunRoutes.Infrastructure.Commands.Tags;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Tags;

[Collection("Database")]
public class UpdateTagCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_タグを更新できる()
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
        const string afterTagName = "タグネーム2";
        TagSummaryDto dto;
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new TagRepository(db);
            var handler = new UpdateTagCommandHandler(repo);
            var command = new UpdateTagCommand(tag.Id, afterTagName, tag.Version);
            dto = await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var updatedTag = await db.Tags.FirstAsync(t => t.Id == tag.Id);
            Assert.Equal(tag.Id, updatedTag.Id);
            Assert.Equal(afterTagName, updatedTag.Name);
            Assert.NotEqual(tag.Version, updatedTag.Version);
            Assert.Equal(dto.RowVersion, updatedTag.Version);
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
            var repo = new TagRepository(db);
            var handler = new UpdateTagCommandHandler(repo);
            var command = new UpdateTagCommand(tag.Id, "タグネーム2", v1);
            await handler.HandleAsync(command, default);
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new TagRepository(db);
            var handler = new UpdateTagCommandHandler(repo);
            var command = new UpdateTagCommand(tag.Id, "タグネーム3", v1);
            await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command, default));
        }
    }

    [Fact]
    public async Task HandleAsync_同名のタグに更新でConflictException()
    {
        // Arrange
        await _fixture.ResetAsync();

        Tag tagA;
        Tag tagB;
        await using (var db = _fixture.CreateDbContext())
        {
            tagA = Tag.Create("タグネーム");
            db.Tags.Add(tagA);
            tagB = Tag.Create("タグネーム2");
            db.Tags.Add(tagB);
            await db.SaveChangesAsync();
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new TagRepository(db);
            var handler = new UpdateTagCommandHandler(repo);
            var command = new UpdateTagCommand(tagB.Id, tagA.Name, tagB.Version);
            await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command, default));
        }
    }
}