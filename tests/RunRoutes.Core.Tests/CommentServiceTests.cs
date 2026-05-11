using Moq;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Dtos;

namespace RunRoutes.Core.Tests;

public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _commentRepoMock = new();
    private readonly Mock<ICourseRepository> _courseRepoMock = new();
    private readonly CommentService _sut;

    public CommentServiceTests()
    {
        _sut = new CommentService(_commentRepoMock.Object, _courseRepoMock.Object);
    }

    private static Comment MakeComment(Guid? userId = null, Guid? courseId = null, Guid? commentId = null)
    {
        return new Comment
        {
            Id = commentId ?? Guid.NewGuid(),
            CourseId = courseId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Body = "Test comment",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    [Fact]
    public async Task Create_正常に投稿できる()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreateCommentRequest("Test comment");

        _courseRepoMock.Setup(r => r.ExistsByIdAsync(courseId)).ReturnsAsync(true);
        _commentRepoMock.Setup(r => r.AddAsync(It.IsAny<Comment>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(courseId, request, userId);

        Assert.Equal("Test comment", result.Comment.Body);
    }

    [Fact]
    public async Task Create_存在しないコースでNotFoundException()
    {
        _courseRepoMock.Setup(r => r.ExistsByIdAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.CreateAsync(Guid.NewGuid(), new CreateCommentRequest("body"), Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_本人が編集できる()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var comment = MakeComment(userId, courseId);
        var request = new UpdateCommentRequest("Updated body");

        _commentRepoMock.Setup(r => r.GetByIdForUpdateAsync(comment.Id)).ReturnsAsync(comment);
        _commentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Comment>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(courseId, comment.Id, request, userId);

        Assert.Equal("Updated body", result.Comment.Body);
    }

    [Fact]
    public async Task Update_他人の編集でForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var comment = MakeComment(ownerId, courseId);
        var request = new UpdateCommentRequest("Updated body");

        _commentRepoMock.Setup(r => r.GetByIdForUpdateAsync(comment.Id)).ReturnsAsync(comment);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.UpdateAsync(courseId, comment.Id, request, Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_本人が削除できる()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var comment = MakeComment(userId, courseId);

        _commentRepoMock.Setup(r => r.GetByIdForUpdateAsync(comment.Id)).ReturnsAsync(comment);
        _commentRepoMock.Setup(r => r.DeleteAsync(comment)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(courseId, comment.Id, userId);

        _commentRepoMock.Verify(r => r.DeleteAsync(comment), Times.Once);
    }

    [Fact]
    public async Task Delete_他人の削除でForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var comment = MakeComment(ownerId, courseId);

        _commentRepoMock.Setup(r => r.GetByIdForUpdateAsync(comment.Id)).ReturnsAsync(comment);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.DeleteAsync(courseId, comment.Id, Guid.NewGuid()));
    }
}
