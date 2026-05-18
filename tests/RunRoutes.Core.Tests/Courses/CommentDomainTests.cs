using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Courses;

public class CommentDomainTests
{
    // ========================================
    // Create
    // ========================================

    [Fact]
    public void Create_正常な引数で生成できる()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var comment = Comment.Create(courseId, userId, "body");

        Assert.NotEqual(Guid.Empty, comment.Id);
        Assert.Equal(courseId, comment.CourseId);
        Assert.Equal(userId, comment.UserId);
        Assert.Equal("body", comment.Body);
    }

    [Fact]
    public void Create_直後は_UpdatedAt_が_null()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), "body");
        Assert.Null(comment.UpdatedAt);
        Assert.False(comment.IsEdited);
    }

    [Fact]
    public void Create_空文字bodyで_ValidationException()
    {
        Assert.Throws<ValidationException>(() =>
            Comment.Create(Guid.NewGuid(), Guid.NewGuid(), "   "));
    }

    // ========================================
    // UpdateBody
    // ========================================

    [Fact]
    public void UpdateBody_本文が更新される()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), "before");
        comment.UpdateBody("after");
        Assert.Equal("after", comment.Body);
    }

    [Fact]
    public void UpdateBody_で_UpdatedAt_に値が入り_IsEdited_が_true()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), "before");
        comment.UpdateBody("after");

        Assert.NotNull(comment.UpdatedAt);
        Assert.True(comment.IsEdited);
    }

    [Fact]
    public void UpdateBody_空文字で_ValidationException()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), "body");
        Assert.Throws<ValidationException>(() => comment.UpdateBody("   "));
    }

    // ========================================
    // Reconstruct
    // ========================================

    [Fact]
    public void Reconstruct_全フィールドが復元される_UpdatedAt_あり()
    {
        var id = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = createdAt.AddHours(2);
        var user = new User { Id = userId, Email = EmailAddress.Create("a@example.com"), Username = Username.Create("usr"), CreatedAt = DateTime.UtcNow };

        var comment = Comment.Reconstruct(id, courseId, userId, "body", createdAt, updatedAt, user);

        Assert.Equal(id, comment.Id);
        Assert.Equal(courseId, comment.CourseId);
        Assert.Equal(userId, comment.UserId);
        Assert.Equal("body", comment.Body);
        Assert.Equal(createdAt, comment.CreatedAt);
        Assert.Equal(updatedAt, comment.UpdatedAt);
        Assert.True(comment.IsEdited);
    }

    [Fact]
    public void Reconstruct_UpdatedAt_が_null_なら_IsEdited_が_false()
    {
        var comment = Comment.Reconstruct(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "body",
            DateTime.UtcNow, null, null);

        Assert.Null(comment.UpdatedAt);
        Assert.False(comment.IsEdited);
    }
}
