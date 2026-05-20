using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Courses;

public class Comment
{
    private Comment() { }

    public Guid Id { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid UserId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Course Course { get; private set; } = null!;
    public User User { get; private set; } = null!;

    public bool IsEdited => UpdatedAt is not null;

    public static Comment Create(Guid courseId, Guid userId, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ValidationException("コメント本文は必須です");

        var now = DateTime.UtcNow;
        return new Comment
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            UserId = userId,
            Body = body,
            CreatedAt = now,
            UpdatedAt = null,
        };
    }

    internal static Comment Reconstruct(
        Guid id,
        Guid courseId,
        Guid userId,
        string body,
        DateTime createdAt,
        DateTime? updatedAt,
        User? user)
    {
        return new Comment
        {
            Id = id,
            CourseId = courseId,
            UserId = userId,
            Body = body,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            User = user!,
        };
    }

    public void UpdateBody(string newBody)
    {
        if (string.IsNullOrWhiteSpace(newBody))
            throw new ValidationException("コメント本文は必須です");
        Body = newBody;
        UpdatedAt = DateTime.UtcNow;
    }
}
