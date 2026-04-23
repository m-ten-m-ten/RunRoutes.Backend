namespace RunRoutes.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
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
