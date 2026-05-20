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
        builder.OwnsOne(u => u.Activation, at =>
        {
            at.Property(x => x.Value).HasColumnName("activation_token");
            at.Property(x => x.ExpiresAt).HasColumnName("activation_token_expires_at");
        });
        builder.OwnsOne(u => u.EmailChange, ecr =>
        {
            ecr.Property(x => x.NewEmail)
                .HasColumnName("pending_email")
                .HasConversion(
                    vo => vo.Value,
                    value => EmailAddress.Create(value));
            ecr.Property(x => x.Token).HasColumnName("email_change_token");
            ecr.Property(x => x.ExpiresAt).HasColumnName("email_change_token_expires_at");
        });
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
