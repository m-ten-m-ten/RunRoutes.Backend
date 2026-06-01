using NetTopologySuite.Geometries;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.UpdateCourse;
using RunRoutes.Core.Courses.Dtos;

namespace RunRoutes.Infrastructure.Commands.Courses;

public class UpdateCourseCommandHandler(ICourseRepository courseRepository)
    : ICommandHandler<UpdateCourseCommand, Guid>
{
    private readonly ICourseRepository _courseRepository = courseRepository;

    public async Task<Guid> HandleAsync(
        UpdateCourseCommand command,
        CancellationToken cancellationToken
    )
    {
        var course = await _courseRepository.GetByIdForUpdateAsync(command.Id)
            ?? throw new NotFoundException("コースが見つかりません");

        if (course.UserId != command.UserId)
            throw new ForbiddenException("このコースを編集する権限がありません");

        if (command.Title is not null)
            course.UpdateTitle(command.Title);

        if (command.Description is not null)
            course.UpdateDescription(command.Description);

        if (command.Difficulty is not null)
            course.ChangeDifficulty(ParseDifficulty(command.Difficulty));

        if (command.IsPublic is not null)
        {
            if (command.IsPublic.Value) course.Publish();
            else course.Unpublish();
        }

        if (command.Route is not null || command.GpxXml is not null)
        {
            var route = ResolveRoute(command.Route, command.GpxXml);
            course.ChangeRoute(route);
        }

        if (command.TagIds is not null)
        {
            var tags = await _courseRepository.GetTagsByIdsForUpdateAsync(command.TagIds);
            course.ReplaceTags(tags);
        }

        await _courseRepository.UpdateAsync(course);
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