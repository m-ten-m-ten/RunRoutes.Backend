using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Courses.Dtos;

namespace RunRoutes.Core.Courses.Commands.UpdateCourse;

public record UpdateCourseCommand(
    Guid Id,
    string? Title,
    string? Description,
    string? Difficulty,
    bool? IsPublic,
    GeoJsonLineStringDto? Route,
    string? GpxXml,
    IEnumerable<Guid>? TagIds,
    Guid UserId
) : ICommand<Guid>;