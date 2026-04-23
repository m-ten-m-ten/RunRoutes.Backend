using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunRoutes.Core.Entities;

namespace RunRoutes.Infrastructure.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Name).HasColumnName("name");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(t => t.Name).IsUnique();
    }
}
