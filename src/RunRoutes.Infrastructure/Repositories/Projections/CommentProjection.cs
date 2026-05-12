using RunRoutes.Core.Courses;
using RunRoutes.Core.Users;

namespace RunRoutes.Infrastructure.Repositories.Projections;

internal sealed class CommentProjection
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public Guid UserId { get; init; }
    public string Body { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public User? User { get; init; }

    public Comment ToDomain() => Comment.Reconstruct(
        Id, CourseId, UserId, Body, CreatedAt, UpdatedAt, User);
}