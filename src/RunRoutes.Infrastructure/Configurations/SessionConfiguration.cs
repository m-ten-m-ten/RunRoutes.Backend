using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunRoutes.Core.Sessions;
using RunRoutes.Core.Users;

namespace RunRoutes.Infrastructure.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(s => s.Id);
        builder.Ignore(s => s.DomainEvents);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.UserId).HasColumnName("user_id");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");

        builder.OwnsOne(s => s.RefreshToken, rt =>
        {
            rt.Property(x => x.Value).HasColumnName("refresh_token");
            rt.Property(x => x.ExpiresAt).HasColumnName("refresh_token_expires_at");
            rt.HasIndex(x => x.Value).IsUnique();
        });
        builder.Navigation(s => s.RefreshToken).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UserId);
    }
}
