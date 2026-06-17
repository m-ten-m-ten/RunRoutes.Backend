using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.CreateComment;
using RunRoutes.Infrastructure.Commands.Courses;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Courses;

[Collection("Database")]
public class CreateCommentCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_コメントを作成できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid userId;
        Guid courseId;
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

            await db.SaveChangesAsync();
        }

        // Act
        Guid commentId;
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new CourseRepository(db);
            var handler = new CreateCommentCommandHandler(repo);
            var command = new CreateCommentCommand(courseId, userId, "テストコメント");
            commentId = await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var comment = await db.Comments.FirstAsync(cm => cm.Id == commentId);
            Assert.Equal("テストコメント", comment.Body);
            Assert.Equal(userId, comment.UserId);
            Assert.False(comment.IsEdited);
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
            var handler = new CreateCommentCommandHandler(repo);
            var command = new CreateCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "テストコメント");
            await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command, default));
        }
    }
}