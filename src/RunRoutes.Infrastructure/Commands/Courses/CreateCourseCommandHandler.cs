using NetTopologySuite.Geometries;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.CreateCourse;
using RunRoutes.Core.Courses.Dtos;

namespace RunRoutes.Infrastructure.Commands.Courses;

public class CreateCourseCommandHandler(ICourseRepository courseRepository)
    : ICommandHandler<CreateCourseCommand, Guid>
{
    private readonly ICourseRepository _courseRepository = courseRepository;

    public async Task<Guid> HandleAsync(
        CreateCourseCommand command,
        CancellationToken cancellationToken
    )
    {
        var difficulty = ParseDifficulty(command.Difficulty);
        var route = ResolveRoute(command.Route, command.GpxXml);
        var tags = await _courseRepository.GetTagsByIdsForUpdateAsync(command.TagIds ?? []);

        var course = Course.Create(
        userId: command.UserId,
        title: command.Title,
        description: command.Description,
        difficulty: difficulty,
        route: route,
        isPublic: command.IsPublic,
        tags: tags);

        await _courseRepository.AddAsync(course);
        return course.Id;
    }

    private static Difficulty ParseDifficulty(string difficulty)
    {
        if (!Enum.TryParse<Difficulty>(difficulty, ignoreCase: true, out var result)
            || !Enum.IsDefined(typeof(Difficulty), result))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["difficulty"] = ["easy, medium, hard のいずれかを指定してください"]
            });
        return result;
    }

    private static LineString ResolveRoute(GeoJsonLineStringDto? geoJson, string? gpxXml)
    {
        if (geoJson is not null)
        {
            var coords = geoJson.Coordinates.Select(c => new Coordinate(c[0], c[1])).ToArray();
            if (coords.Length < 2)
                throw new ValidationException("ルートには2点以上の座標が必要です");
            return new LineString(coords) { SRID = 4326 };
        }

        if (gpxXml is not null)
            return GpxParser.Parse(gpxXml);

        throw new ValidationException("route または gpxXml のいずれかを指定してください");
    }
}