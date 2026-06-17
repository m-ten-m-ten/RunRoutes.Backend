using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.UpdateComment;
using RunRoutes.Infrastructure.Commands.Courses;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Courses;

[Collection("Database")]
public class UpdateCommentCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_コメントを編集できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid userId;
        Guid courseId;
        Guid commentId;
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
            courseId = course.Id;

            var comment = course.AddComment(userId, "テストコメント");
            commentId = comment.Id;

            await db.SaveChangesAsync();
        }

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new CourseRepository(db);
            var handler = new UpdateCommentCommandHandler(repo);
            var command = new UpdateCommentCommand(courseId, commentId, userId, "テストコメント編集後");
            await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var comment = await db.Comments.FirstAsync(cm => cm.Id == commentId);
            Assert.Equal("テストコメント編集後", comment.Body);
            Assert.Equal(userId, comment.UserId);
            Assert.True(comment.IsEdited);
        }
    }

    [Fact]
    public async Task HandleAsync_存在しないコースでNotFoundException()
    {
        // Arrange
        await _fixture.ResetAsync();

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new CourseRepository(db);
            var handler = new UpdateCommentCommandHandler(repo);
            var command = new UpdateCommentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "テストコメント編集後");
            await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command, default));
        }
    }
}