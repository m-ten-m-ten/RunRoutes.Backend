using NetTopologySuite.Geometries;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Users;

namespace RunRoutes.Infrastructure.Repositories.Projections;

// EF Core の射影専用 DTO。Course の組み立て前の入れ物。
// ここから Course.Reconstruct を呼んで集約を組み立てる。
internal sealed class CourseProjection
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Difficulty Difficulty { get; init; }
    public LineString Route { get; init; } = null!;
    public Distance Distance { get; init; } = null!;
    public bool IsPublic { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public User User { get; init; } = null!;
    public List<CommentProjection> Comments { get; init; } = [];
    public List<TagProjection> Tags { get; init; } = [];
    public int CommentCount { get; init; }

    public Course ToDomain() => Course.Reconstruct(
        Id, UserId, Title, Description, Difficulty, Route, Distance, IsPublic,
        CreatedAt, UpdatedAt, User,
        Comments.Select(c => c.ToDomain()),
        Tags.Select(t => t.ToDomain()),
        CommentCount);
}
