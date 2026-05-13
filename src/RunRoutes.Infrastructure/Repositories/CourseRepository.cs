using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Tags;
using RunRoutes.Infrastructure.Data;
using RunRoutes.Infrastructure.Repositories.Projections;

namespace RunRoutes.Infrastructure.Repositories;

public class CourseRepository(AppDbContext db) : ICourseRepository
{
    public async Task<(IEnumerable<Course> Courses, int TotalCount)> GetListAsync(GetCoursesQuery query, Guid? currentUserId)
    {
        var q = db.Courses
            .AsNoTracking()
            .Where(c => c.IsPublic || c.UserId == currentUserId);

        if (query.Difficulty is not null)
        {
            if (Enum.TryParse<Difficulty>(query.Difficulty, ignoreCase: true, out var parsedDifficulty)
                && Enum.IsDefined(typeof(Difficulty), parsedDifficulty))
                q = q.Where(c => c.Difficulty == parsedDifficulty);
            else
                q = q.Where(c => false);
        }

        if (query.TagIds is not null && query.TagIds.Any())
            q = q.Where(c => c.Tags.Any(t => query.TagIds.Contains(t.Id)));

        if (query.Lat is not null && query.Lng is not null && query.RadiusKm is not null)
        {
            var center = new Point(query.Lng.Value, query.Lat.Value) { SRID = 4326 };
            q = q.Where(c => c.Route.Distance(center) * 111.139 <= query.RadiusKm.Value);
        }

        var totalCount = await q.CountAsync();

        var projections = await q
            .OrderByDescending(c => c.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
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
                // 一覧では Comments は不要なので空
            })
            .ToListAsync();

        var courses = projections.Select(p => p.ToDomain()).ToList();
        return (courses, totalCount);
    }

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
