using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RunRoutes.Core.DTOs.Courses;
using RunRoutes.Core.Entities;
using RunRoutes.Core.Interfaces.Repositories;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Repositories;

public class CourseRepository(AppDbContext db) : ICourseRepository
{
    public async Task<(IEnumerable<Course> Courses, int TotalCount)> GetListAsync(GetCoursesQuery query, Guid? currentUserId)
    {
        var q = db.Courses
            .AsNoTracking()
            .Where(c => c.IsPublic || c.UserId == currentUserId);

        if (query.Difficulty is not null)
            q = q.Where(c => c.Difficulty == query.Difficulty);

        if (query.TagIds is not null && query.TagIds.Any())
            q = q.Where(c => c.Tags.Any(t => query.TagIds.Contains(t.Id)));

        if (query.Lat is not null && query.Lng is not null && query.RadiusKm is not null)
        {
            var center = new Point(query.Lng.Value, query.Lat.Value) { SRID = 4326 };
            q = q.Where(c => c.Route.Distance(center) * 111.139 <= query.RadiusKm.Value);
        }

        var totalCount = await q.CountAsync();

        var courses = await q
            .OrderByDescending(c => c.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new Course
            {
                Id = c.Id,
                UserId = c.UserId,
                Title = c.Title,
                Description = c.Description,
                Difficulty = c.Difficulty,
                Route = c.Route,
                DistanceM = c.DistanceM,
                IsPublic = c.IsPublic,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                User = c.User,
                Tags = c.Tags.ToList(),
                CommentCount = c.Comments.Count,
            })
            .ToListAsync();

        return (courses, totalCount);
    }

    public Task<Course?> GetByIdAsync(Guid id) =>
        db.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new Course
            {
                Id = c.Id,
                UserId = c.UserId,
                Title = c.Title,
                Description = c.Description,
                Difficulty = c.Difficulty,
                Route = c.Route,
                DistanceM = c.DistanceM,
                IsPublic = c.IsPublic,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                User = c.User,
                Tags = c.Tags.ToList(),
                CommentCount = c.Comments.Count,
            })
            .FirstOrDefaultAsync();

    public Task<Course?> GetByIdForUpdateAsync(Guid id) =>
        db.Courses
            .Include(c => c.User)
            .Include(c => c.Tags)
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
        db.Courses.Update(course);
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
