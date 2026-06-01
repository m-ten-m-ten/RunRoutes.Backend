using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Courses.Dtos;

namespace RunRoutes.Core.Courses.Queries.ListCourses;

public record ListCoursesQuery(
    double? Lat,
    double? Lng,
    double? RadiusKm,
    string? Difficulty,
    IEnumerable<Guid>? TagIds,
    int Page,
    int PageSize,
    Guid? CurrentUserId
) : IQuery<GetCoursesResponse>;