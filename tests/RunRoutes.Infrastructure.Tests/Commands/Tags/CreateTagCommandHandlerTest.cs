using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Tags.Commands.CreateTag;
using RunRoutes.Core.Tags.Dtos;
using RunRoutes.Infrastructure.Commands.Tags;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Tags;

[Collection("Database")]
public class CreateTagCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_タグを作成できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        // Act
        TagSummaryDto dto;
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new TagRepository(db);
            var handler = new CreateTagCommandHandler(repo);
            var command = new CreateTagCommand("タグネーム");
            dto = await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var tag = await db.Tags.FirstAsync(t => t.Id == dto.Id);
            Assert.Equal(dto.Name, tag.Name);
            Assert.Equal(dto.RowVersion, tag.Version);
        }
    }
    [Fact]
    public async Task HandleAsync_同名のタグを登録でConflictException()
    {
        // Arrange
        await _fixture.ResetAsync();

        const string TagName = "タグネーム";
        await using (var db = _fixture.CreateDbContext())
        {
            var tag = Tag.Create(TagName);
            db.Tags.Add(tag);
            await db.SaveChangesAsync();
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new TagRepository(db);
            var handler = new CreateTagCommandHandler(repo);
            var command = new CreateTagCommand(TagName);
            await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(command, default));
        }
    }
}