using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;
using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Courses;

public class Course : AggregateRoot
{
    // EF Core 用の parameterless constructor(リフレクションで呼ばれる)
    private Course() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public LineString Route { get; private set; } = null!;
    public Distance Distance { get; private set; } = null!;
    public bool IsPublic { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public User User { get; private set; } = null!;
    public ICollection<Comment> Comments { get; private set; } = [];
    public ICollection<Tag> Tags { get; private set; } = [];

    [NotMapped]
    public int CommentCount { get; private set; }

    // ========================================
    // ファクトリメソッド(新規生成)
    // ========================================
    public static Course Create(
        Guid userId,
        string title,
        string? description,
        Difficulty difficulty,
        LineString route,
        bool isPublic,
        IEnumerable<Tag> tags)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("タイトルは必須です");
        if (route.NumPoints < 2)
            throw new ValidationException("ルートには2点以上の座標が必要です");

        var now = DateTime.UtcNow;
        return new Course
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Description = description,
            Difficulty = difficulty,
            Route = route,
            Distance = CalculateDistance(route),
            IsPublic = isPublic,
            Tags = tags.ToList(),
            Comments = [],
            CreatedAt = now,
            UpdatedAt = now,
        };
    }


    // ========================================
    // 再構成メソッド(EF Core / Repository から)
    // ========================================
    internal static Course Reconstruct(
        Guid id,
        Guid userId,
        string title,
        string? description,
        Difficulty difficulty,
        LineString route,
        Distance distance,
        bool isPublic,
        DateTime createdAt,
        DateTime updatedAt,
        User user,
        IEnumerable<Comment> comments,
        IEnumerable<Tag> tags,
        int commentCount)
    {
        return new Course
        {
            Id = id,
            UserId = userId,
            Title = title,
            Description = description,
            Difficulty = difficulty,
            Route = route,
            Distance = distance,
            IsPublic = isPublic,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            User = user,
            Comments = comments.ToList(),
            Tags = tags.ToList(),
            CommentCount = commentCount,
        };
    }

    // ========================================
    // ドメインメソッド(状態変更)
    // ========================================
    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ValidationException("タイトルは必須です");
        Title = newTitle;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string? newDescription)
    {
        Description = newDescription;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeDifficulty(Difficulty newDifficulty)
    {
        Difficulty = newDifficulty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeRoute(LineString newRoute)
    {
        if (newRoute.NumPoints < 2)
            throw new ValidationException("ルートには2点以上の座標が必要です");
        Route = newRoute;
        Distance = CalculateDistance(newRoute);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        IsPublic = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unpublish()
    {
        IsPublic = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReplaceTags(IEnumerable<Tag> newTags)
    {
        Tags = newTags.ToList();
        UpdatedAt = DateTime.UtcNow;
    }

    // ========================================
    // コメント関連(既存メソッド + Comment.Create 経由に変更)
    // ========================================
    public Comment AddComment(Guid authorId, string body)
    {
        var comment = Comment.Create(this.Id, authorId, body);
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

    // ========================================
    // 距離計算(CourseService から移植)
    // ========================================
    private static Distance CalculateDistance(LineString route)
    {
        double total = 0;
        for (int i = 0; i < route.Coordinates.Length - 1; i++)
        {
            total += HaversineDistance(route.Coordinates[i], route.Coordinates[i + 1]);
        }
        return Distance.FromMeters(total);
    }

    private static double HaversineDistance(Coordinate a, Coordinate b)
    {
        const double R = 6371000;
        var lat1 = a.Y * Math.PI / 180;
        var lat2 = b.Y * Math.PI / 180;
        var dLat = (b.Y - a.Y) * Math.PI / 180;
        var dLon = (b.X - a.X) * Math.PI / 180;
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }
}
