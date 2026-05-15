using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Tags;
using RunRoutes.Infrastructure.Configurations.Converters;

namespace RunRoutes.Infrastructure.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");

        builder.HasKey(c => c.Id);
        builder.Ignore(c => c.DomainEvents);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.UserId).HasColumnName("user_id");
        builder.Property(c => c.Title).HasColumnName("title");
        builder.Property(c => c.Description).HasColumnName("description");
        builder.Property(c => c.Difficulty).HasColumnName("difficulty").HasConversion(new DifficultyConverter());
        builder.Property(c => c.Route).HasColumnName("route")
            .HasColumnType("geometry(LineString,4326)");
        builder.Property(c => c.Distance).HasColumnName("distance_m").HasConversion(new DistanceConverter());
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

        // バッキングフィールドの明示
        builder.Navigation(c => c.Tags)
            .HasField("_tags")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(c => c.Comments)
            .HasField("_comments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
