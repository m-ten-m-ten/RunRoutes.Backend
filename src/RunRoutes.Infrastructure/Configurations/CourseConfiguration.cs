using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunRoutes.Core.Entities;

namespace RunRoutes.Infrastructure.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.UserId).HasColumnName("user_id");
        builder.Property(c => c.Title).HasColumnName("title");
        builder.Property(c => c.Description).HasColumnName("description");
        builder.Property(c => c.Difficulty).HasColumnName("difficulty");
        builder.Property(c => c.Route).HasColumnName("route")
            .HasColumnType("geometry(LineString,4326)");
        builder.Property(c => c.DistanceM).HasColumnName("distance_m");
        builder.Property(c => c.IsPublic).HasColumnName("is_public");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(c => c.User)
            .WithMany(u => u.Courses)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Tags)
            .WithMany(t => t.Courses)
            .UsingEntity("course_tags",
                right => right.HasOne(typeof(Tag))
                    .WithMany()
                    .HasForeignKey("tag_id")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne(typeof(Course))
                    .WithMany()
                    .HasForeignKey("course_id")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("course_tags");
                    join.HasKey("course_id", "tag_id");
                }
            );
    }
}
