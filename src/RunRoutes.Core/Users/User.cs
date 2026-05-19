using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;

namespace RunRoutes.Core.Users;

public class User : AggregateRoot
{
    // === Step 2 で private set 化(このステップで触る範囲) ===
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public UserRole Role { get; private set; } = UserRole.User;
    public ActivationToken? Activation { get; private set; }  // ★ VO に統合

    // === Step 3 で private set 化 ===
    public EmailAddress Email { get; private set; } = null!;
    public EmailChangeRequest? EmailChange { get; private set; }

    // === Step 4 で新たに private set 化 ===
    public Username Username { get; private set; } = null!;
    public HashedPassword PasswordHash { get; private set; } = null!;
    public bool IsActive { get; private set; }

    // === Step 5 で削除/private set 化予定なので public set 据え置き ===
    public DateTime UpdatedAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    // === Step 5 で private set 化予定(EF Core が触るのみなので最終で十分) ===
    public ICollection<Course> Courses { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];

    private User() { } // EF Core 用

    public static User Register(
        EmailAddress email,
        Username username,
        PlainPassword password,
        IPasswordHasher hasher,
        DateTime now,
        TimeSpan activationValidity)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            PasswordHash = hasher.Hash(password),
            IsActive = false,
            Activation = ActivationToken.Generate(now, activationValidity),
            CreatedAt = now,
            UpdatedAt = now,
            Role = UserRole.User,
        };
    }

    public void Activate(DateTime now)
    {
        if (IsActive)
            throw new ValidationException("アカウントは既に有効化されています");
        if (Activation is null)
            throw new ValidationException("有効化トークンがありません");
        if (Activation.IsExpired(now))
            throw new ValidationException("有効化トークンの有効期限が切れています");

        IsActive = true;
        Activation = null;
        UpdatedAt = now;
    }

    public void PromoteToAdmin(DateTime now)
    {
        Role = UserRole.Admin;
        UpdatedAt = now;
    }

    public void RequestEmailChange(
        EmailAddress newEmail,
        PlainPassword currentPassword,
        IPasswordHasher hasher,
        DateTime now,
        TimeSpan validity)
    {
        if (!hasher.Verify(currentPassword, PasswordHash))
            throw new ValidationException("現在のパスワードが正しくありません");

        EmailChange = EmailChangeRequest.Create(newEmail, now, validity);
        UpdatedAt = now;
    }

    public void ConfirmEmailChange(string token, DateTime now)
    {
        if (EmailChange is null)
            throw new ValidationException("メール変更要求がありません");
        if (EmailChange.Token != token)
            throw new ValidationException("メール変更トークンが無効です");
        if (EmailChange.IsExpired(now))
            throw new ValidationException("メール変更トークンの有効期限が切れています");

        Email = EmailChange.NewEmail;
        EmailChange = null;
        UpdatedAt = now;
    }

    public bool VerifyPassword(PlainPassword password, IPasswordHasher hasher) =>
        hasher.Verify(password, PasswordHash);

    public void ChangeUsername(Username newUsername, DateTime now)
    {
        Username = newUsername;
        UpdatedAt = now;
    }

    public void ChangePassword(
        PlainPassword currentPassword,
        PlainPassword newPassword,
        IPasswordHasher hasher,
        DateTime now)
    {
        if (!hasher.Verify(currentPassword, PasswordHash))
            throw new ValidationException("現在のパスワードが正しくありません");

        PasswordHash = hasher.Hash(newPassword);
        UpdatedAt = now;
    }
}
