using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Tags;
using RunRoutes.Infrastructure.Data;
using RunRoutes.Infrastructure.Repositories.Projections;

namespace RunRoutes.Infrastructure.Repositories;

public class CourseRepository(AppDbContext db) : ICourseRepository
{
    public async Task<Course?> GetByIdAsync(Guid id)
    {
        var projection = await db.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseProjection
            {
                Id = c.Id,
                UserId = c.UserId,
                Title = c.Title,
                Description = c.Description,
                Difficulty = c.Difficulty,
                Route = c.Route,
                Distance = c.Distance,
                IsPublic = c.IsPublic,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                User = c.User,
                Tags = c.Tags.Select(t => new TagProjection
                {
                    Id = t.Id,
                    Name = t.Name,
                    CreatedAt = t.CreatedAt,
                    Version = t.Version,
                }).ToList(),
                CommentCount = c.Comments.Count,
                Comments = c.Comments
                    .OrderBy(cm => cm.CreatedAt)
                    .Select(cm => new CommentProjection
                    {
                        Id = cm.Id,
                        CourseId = cm.CourseId,
                        UserId = cm.UserId,
                        Body = cm.Body,
                        CreatedAt = cm.CreatedAt,
                        UpdatedAt = cm.UpdatedAt,
                        User = cm.User,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync();

        return projection?.ToDomain();
    }

    public Task<Course?> GetByIdForUpdateAsync(Guid id) =>
        db.Courses
            .Include(c => c.User)
            .Include(c => c.Tags)
            .Include(c => c.Comments).ThenInclude(cm => cm.User)
            .FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> ExistsByIdAsync(Guid id) =>
        db.Courses.AnyAsync(c => c.Id == id);

    public async Task AddAsync(Course course)
    {
        db.Courses.Add(course);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Course course)
    {
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Course course)
    {
        db.Courses.Remove(course);
        await db.SaveChangesAsync();
    }

    public async Task<IEnumerable<Tag>> GetTagsByIdsForUpdateAsync(IEnumerable<Guid> tagIds)
    {
        var ids = tagIds.ToList();
        return await db.Tags.Where(t => ids.Contains(t.Id)).ToListAsync();
    }
}
