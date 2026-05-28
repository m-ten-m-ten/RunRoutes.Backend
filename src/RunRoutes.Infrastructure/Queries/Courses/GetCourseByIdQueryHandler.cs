using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Courses.Queries.GetCourseById;
using RunRoutes.Core.Tags.Dtos;
using RunRoutes.Core.Users.Dtos;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Queries.Courses;

public class GetCourseByIdQueryHandler(AppDbContext db)
    : IQueryHandler<GetCourseByIdQuery, GetCourseResponse>
{
    private readonly AppDbContext _db = db;

    public async Task<GetCourseResponse> HandleAsync(
        GetCourseByIdQuery query,
        CancellationToken cancellationToken)
    {
        var raw = await _db.Courses
            .AsNoTracking()
            .Where(c => c.Id == query.CourseId)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                c.Difficulty,
                DistanceM = c.Distance.Meters,
                c.IsPublic,
                c.Route,
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
                c.UpdatedAt,
                c.UserId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (raw is null)
            throw new NotFoundException("コースが見つかりません");

        if (!raw.IsPublic && raw.UserId != query.CurrentUserId)
            throw new ForbiddenException("このコースにアクセスする権限がありません");

        var dto = new CourseDetailDto(
            raw.Id,
            raw.Title,
            raw.Description,
            raw.Difficulty.ToString().ToLowerInvariant(),
            raw.DistanceM,
            raw.IsPublic,
            new GeoJsonLineStringDto("LineString", raw.Route.Coordinates.Select(coord => new[] { coord.X, coord.Y })),
            raw.User,
            raw.Tags,
            raw.CommentCount,
            raw.CreatedAt,
            raw.UpdatedAt);

        return new GetCourseResponse(dto);
    }
}