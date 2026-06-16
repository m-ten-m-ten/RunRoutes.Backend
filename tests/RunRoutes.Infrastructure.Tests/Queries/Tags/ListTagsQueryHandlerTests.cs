using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Tags.Dtos;
using RunRoutes.Core.Tags.Queries.ListTags;
using RunRoutes.Infrastructure.Queries.Tags;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Tags;

[Collection("Database")]
public class ListTagsQueryHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_名前順に全件が取得できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        await using (var db = _fixture.CreateDbContext())
        {
            var tagB = Tag.Create("B_Running");
            db.Tags.Add(tagB);
            var tagC = Tag.Create("C_Trail");
            db.Tags.Add(tagC);
            var tagA = Tag.Create("A_Walking");
            db.Tags.Add(tagA);

            await db.SaveChangesAsync();
        }

        // Act
        IEnumerable<TagSummaryDto> response;
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new ListTagsQueryHandler(db);
            var query = new ListTagsQuery();
            response = await handler.HandleAsync(query, default);
        }

        // Assert
        var tagList = response.ToList();
        List<string> tagNames = ["A_Walking", "B_Running", "C_Trail"];
        Assert.Equal(tagNames, tagList.Select(t => t.Name).ToList());
        await using (var db = _fixture.CreateDbContext())
        {
            var tagA = db.Tags.Where(t => t.Name == "A_Walking").Single();
            Assert.Equal(tagA.Version, tagList[0].RowVersion);
        }
    }

    [Fact]
    public async Task HandleAsync_タグ0件で空のコレクションが返る()
    {
        // Arrange
        await _fixture.ResetAsync();

        // Act
        IEnumerable<TagSummaryDto> response;
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new ListTagsQueryHandler(db);
            var query = new ListTagsQuery();
            response = await handler.HandleAsync(query, default);
        }

        // Assert
        Assert.Empty(response);
    }
}