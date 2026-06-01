using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Courses.Queries.ListCourses;
using RunRoutes.Core.Tags.Dtos;
using RunRoutes.Core.Users.Dtos;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Queries.Courses;

public class ListCoursesQueryHandler(AppDbContext db) : IQueryHandler<ListCoursesQuery, GetCoursesResponse>
{
    private readonly AppDbContext _db = db;

    public async Task<GetCoursesResponse> HandleAsync(ListCoursesQuery query, CancellationToken cancellationToken)
    {
        var q = _db.Courses
            .AsNoTracking()
            .Where(c => c.IsPublic || c.UserId == query.CurrentUserId);

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

        var courses = await q
            .OrderByDescending(c => c.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Difficulty,
                DistanceM = c.Distance.Meters,
                c.IsPublic,
                User = new UserDto(
                        c.User.Id,
                        c.User.Email.Value,
                        c.User.Username.Value,
                        c.User.Role.ToString(),
                        c.User.CreatedAt
                    ),
                Tags = c.Tags.Select(t => new TagDto(t.Id, t.Name)),
                CommentCount = c.Comments.Count,
                c.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var dtos = courses.Select(c =>
            new CourseListItemDto(
                c.Id,
                c.Title,
                c.Difficulty.ToString().ToLowerInvariant(),
                c.DistanceM,
                c.IsPublic,
                c.User,
                c.Tags,
                c.CommentCount,
                c.CreatedAt
            )
        );

        return new GetCoursesResponse(dtos, totalCount, query.Page, query.PageSize);
    }
}