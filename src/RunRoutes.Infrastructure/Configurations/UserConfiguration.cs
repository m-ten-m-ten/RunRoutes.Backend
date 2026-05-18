using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunRoutes.Core.Users;

namespace RunRoutes.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Ignore(u => u.DomainEvents);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasConversion(
                vo => vo.Value,
                value => EmailAddress.Create(value)
            );
        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasConversion(
                vo => vo.Value,
                value => Username.Create(value)
            );
        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasConversion(
                vo => vo.Value,
                value => HashedPassword.FromHash(value)
            );
        builder.Property(u => u.IsActive).HasColumnName("is_active");
        builder.Property(u => u.ActivationToken).HasColumnName("activation_token");
        builder.Property(u => u.ActivationTokenExpiresAt).HasColumnName("activation_token_expires_at");
        builder.Property(u => u.PendingEmail).HasColumnName("pending_email");
        builder.Property(u => u.EmailChangeToken).HasColumnName("email_change_token");
        builder.Property(u => u.EmailChangeTokenExpiresAt).HasColumnName("email_change_token_expires_at");
        builder.Property(u => u.RefreshToken).HasColumnName("refresh_token");
        builder.Property(u => u.RefreshTokenExpiresAt).HasColumnName("refresh_token_expires_at");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasConversion<int>()
            .HasDefaultValue(UserRole.User);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();
    }
}
