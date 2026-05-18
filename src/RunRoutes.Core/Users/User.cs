using RunRoutes.Core.Common;
using RunRoutes.Core.Courses;

namespace RunRoutes.Core.Users;

public class User : AggregateRoot
{
    public Guid Id { get; set; }
    public EmailAddress Email { get; set; } = null!;
    public Username Username { get; set; } = null!;
    public HashedPassword PasswordHash { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? ActivationToken { get; set; }
    public DateTime? ActivationTokenExpiresAt { get; set; }
    public string? PendingEmail { get; set; }
    public string? EmailChangeToken { get; set; }
    public DateTime? EmailChangeTokenExpiresAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserRole Role { get; set; } = UserRole.User;

    public ICollection<Course> Courses { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
}
