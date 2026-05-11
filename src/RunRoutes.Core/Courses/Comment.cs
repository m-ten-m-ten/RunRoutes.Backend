using RunRoutes.Core.Users;

namespace RunRoutes.Core.Courses;

public class Comment
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Course Course { get; set; } = null!;
    public User User { get; set; } = null!;

    public void UpdateBody(string newBody)
    {
        Body = newBody;
        UpdatedAt = DateTime.UtcNow;
    }
}
