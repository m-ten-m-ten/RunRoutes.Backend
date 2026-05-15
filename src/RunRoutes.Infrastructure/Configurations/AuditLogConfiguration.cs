using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunRoutes.Core.Audit;

namespace RunRoutes.Infrastructure.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(a => a.EventType).HasColumnName("event_type");
        builder.Property(a => a.ActorId).HasColumnName("actor_id").IsRequired(false);
        builder.Property(a => a.TargetType).HasColumnName("target_type");
        builder.Property(a => a.TargetId).HasColumnName("target_id");
        builder.Property(a => a.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(a => a.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => a.ActorId);
        builder.HasIndex(a => new { a.TargetType, a.TargetId });
    }
}