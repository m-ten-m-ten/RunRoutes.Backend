using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace RunRoutes.Core.Entities;

public class Course
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public LineString Route { get; set; } = null!;
    public double DistanceM { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];

    [NotMapped]
    public int CommentCount { get; set; }
}
