using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;
using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Courses;

public class Course : AggregateRoot
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Difficulty Difficulty { get; set; }
    public LineString Route { get; set; } = null!;
    public Distance Distance { get; set; } = null!;
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];

    [NotMapped]
    public int CommentCount { get; set; }

    public Comment AddComment(Guid authorId, string body)
    {
        var now = DateTime.UtcNow;
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            CourseId = this.Id,
            UserId = authorId,
            Body = body,
            CreatedAt = now,
            UpdatedAt = now,
        };
        Comments.Add(comment);
        return comment;
    }

    public Comment EditComment(Guid commentId, Guid editorId, string newBody)
    {
        var comment = Comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw new NotFoundException("コメントが見つかりません");

        if (comment.UserId != editorId)
            throw new ForbiddenException("このコメントを編集する権限がありません");

        comment.UpdateBody(newBody);
        return comment;
    }

    public void RemoveComment(Guid commentId, Guid removerId)
    {
        var comment = Comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw new NotFoundException("コメントが見つかりません");

        if (comment.UserId != removerId)
            throw new ForbiddenException("このコメントを削除する権限がありません");

        Comments.Remove(comment);
    }
}
