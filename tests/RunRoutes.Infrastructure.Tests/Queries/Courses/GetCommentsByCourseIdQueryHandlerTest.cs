using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Courses.Queries.GetComments;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Queries.Courses;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Queries.Courses;

[Collection("Database")]
public class GetCommentsByCourseIdQueryHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_CreatedAt昇順に全件が取得できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid courseId;
        Guid userId;
        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated();
            db.Users.Add(user);
            userId = user.Id;

            var course = Course.Create(
                userId: user.Id,
                title: "テストコース",
                description: "テストコースの説明",
                difficulty: Difficulty.Easy,
                route: TestCourseBuilder.CreateTestRoute(),
                isPublic: true,
                tags: []);
            db.Courses.Add(course);

            await db.SaveChangesAsync();
            courseId = course.Id;

            // 時間差をつけるために、コメント1件ごとに保存
            course.AddComment(user.Id, "テストコメント1");
            await db.SaveChangesAsync();

            course.AddComment(user.Id, "テストコメント2");
            await db.SaveChangesAsync();
        }

        // Act
        GetCommentsResponse response;
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new GetCommentsByCourseIdQueryHandler(db);
            var command = new GetCommentsByCourseIdQuery(courseId);
            response = await handler.HandleAsync(command, default);
        }

        // Assert
        var comments = response.Comments.ToList();
        Assert.Equal("テストコメント1", comments[0].Body);
        Assert.Equal("テストコメント2", comments[1].Body);
        Assert.Equal(userId, comments[0].User.Id);
        Assert.False(comments[0].IsEdited);
    }

    [Fact]
    public async Task HandleAsync_存在しないコースでNotFoundException()
    {
        // Arrange
        await _fixture.ResetAsync();

        // Act & Assert
        await using var db = _fixture.CreateDbContext();
        var handler = new GetCommentsByCourseIdQueryHandler(db);
        var command = new GetCommentsByCourseIdQuery(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
        handler.HandleAsync(command, default));
    }

    [Fact]
    public async Task HandleAsync_コメント0件で空のコレクションが返る()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid courseId;
        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated();
            db.Users.Add(user);

            var course = Course.Create(
                userId: user.Id,
                title: "テストコース",
                description: "テストコースの説明",
                difficulty: Difficulty.Easy,
                route: TestCourseBuilder.CreateTestRoute(),
                isPublic: true,
                tags: []);
            db.Courses.Add(course);

            await db.SaveChangesAsync();
            courseId = course.Id;
        }

        // Act
        GetCommentsResponse response;
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new GetCommentsByCourseIdQueryHandler(db);
            var command = new GetCommentsByCourseIdQuery(courseId);
            response = await handler.HandleAsync(command, default);
        }

        // Assert
        var comments = response.Comments.ToList();
        Assert.Empty(comments);
    }

}
