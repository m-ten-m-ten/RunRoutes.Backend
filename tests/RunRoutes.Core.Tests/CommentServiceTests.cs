using Moq;
using NetTopologySuite.Geometries;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests;

public class CommentServiceTests
{
    private readonly Mock<ICourseRepository> _courseRepoMock = new();
    private readonly CommentService _sut;

    public CommentServiceTests()
    {
        _sut = new CommentService(_courseRepoMock.Object);
    }

    public static Course MakeCourse(Guid? courseId = null, Guid? ownerId = null, List<Comment>? comments = null)
    {
        var uid = ownerId ?? Guid.NewGuid();
        var cid = courseId ?? Guid.NewGuid();
        var user = new User { Id = uid, Email = EmailAddress.Create("a@example.com"), Username = Username.Create("user"), CreatedAt = DateTime.UtcNow };

        // comments を持つテスト用 Course は Reconstruct 経由で組み立てる(論点 B-3)
        if (comments is not null)
        {
            // ダミーの Route / Distance を用意(テストでは Route の中身は使わない)
            var route = new LineString([new Coordinate(135.0, 35.0), new Coordinate(135.1, 35.1)]) { SRID = 4326 };
            var distance = Distance.FromMeters(100);
            var now = DateTime.UtcNow;

            return Course.Reconstruct(
                id: cid,
                userId: uid,
                title: "Test Course",
                description: null,
                difficulty: Difficulty.Easy,
                route: route,
                distance: distance,
                isPublic: true,
                createdAt: now,
                updatedAt: now,
                user: user,
                comments: comments,
                tags: [],
                commentCount: comments.Count);
        }

        // 通常の(comments を持たない)テストは Create 経由で組み立てる
        var course = Course.Create(
            userId: uid,
            title: "Test Course",
            description: null,
            difficulty: Difficulty.Easy,
            route: new LineString([new Coordinate(135.0, 35.0), new Coordinate(135.1, 35.1)]) { SRID = 4326 },
            isPublic: true,
            tags: []);

        if (courseId is not null)
            SetPrivate(course, nameof(Course.Id), courseId.Value);

        SetPrivate(course, nameof(Course.User), user);

        return course;
    }

    private static void SetPrivate<T>(T target, string propertyName, object value) where T : class
    {
        typeof(T).GetProperty(propertyName)!.SetValue(target, value);
    }

    private static Comment MakeComment(Guid? userId = null, Guid? courseId = null, Guid? commentId = null)
    {
        var comment = Comment.Create(
            courseId: courseId ?? Guid.NewGuid(),
            userId: userId ?? Guid.NewGuid(),
            body: "Test comment");

        if (commentId is not null)
            SetPrivate(comment, nameof(Comment.Id), commentId.Value);

        return comment;
    }

    [Fact]
    public async Task Create_正常に投稿できる()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var course = MakeCourse(courseId);
        var request = new CreateCommentRequest("Test comment");

        _courseRepoMock.Setup(r => r.GetByIdForUpdateAsync(courseId)).ReturnsAsync(course);
        _courseRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Course>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(courseId, request, userId);

        Assert.Equal("Test comment", result.Comment.Body);
    }

    [Fact]
    public async Task Create_存在しないコースでNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>())).ReturnsAsync((Course?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.CreateAsync(Guid.NewGuid(), new CreateCommentRequest("body"), Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_本人が編集できる()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var comment = MakeComment(userId, courseId);
        var course = MakeCourse(courseId, comments: [comment]);
        var request = new UpdateCommentRequest("Updated body");

        _courseRepoMock.Setup(r => r.GetByIdForUpdateAsync(courseId)).ReturnsAsync(course);
        _courseRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Course>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(courseId, comment.Id, request, userId);

        Assert.Equal("Updated body", result.Comment.Body);
    }

    [Fact]
    public async Task Update_他人の編集でForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var comment = MakeComment(ownerId, courseId);
        var course = MakeCourse(courseId, comments: [comment]);
        var request = new UpdateCommentRequest("Updated body");

        _courseRepoMock.Setup(r => r.GetByIdForUpdateAsync(courseId)).ReturnsAsync(course);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.UpdateAsync(courseId, comment.Id, request, Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_本人が削除できる()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var comment = MakeComment(userId, courseId);
        var course = MakeCourse(courseId, comments: [comment]);

        _courseRepoMock.Setup(r => r.GetByIdForUpdateAsync(courseId)).ReturnsAsync(course);
        _courseRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Course>())).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(courseId, comment.Id, userId);

        Assert.DoesNotContain(comment, course.Comments);
    }

    [Fact]
    public async Task Delete_他人の削除でForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var comment = MakeComment(ownerId, courseId);
        var course = MakeCourse(courseId, comments: [comment]);

        _courseRepoMock.Setup(r => r.GetByIdForUpdateAsync(courseId)).ReturnsAsync(course);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.DeleteAsync(courseId, comment.Id, Guid.NewGuid()));
    }
}
