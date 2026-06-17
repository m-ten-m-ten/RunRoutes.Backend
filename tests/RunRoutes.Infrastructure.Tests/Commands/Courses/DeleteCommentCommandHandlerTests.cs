using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.DeleteComment;
using RunRoutes.Infrastructure.Commands.Courses;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Courses;

[Collection("Database")]
public class DeleteCommentCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_コメントを削除できる()
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
            var handler = new DeleteCommentCommandHandler(repo);
            var command = new DeleteCommentCommand(courseId, commentId, userId);
            await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var comment = await db.Comments.FirstOrDefaultAsync(cm => cm.Id == commentId);
            Assert.Null(comment);

            // コースは残っていることを確認
            var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            Assert.NotNull(course);
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
            var handler = new DeleteCommentCommandHandler(repo);
            var command = new DeleteCommentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command, default));
        }
    }
}