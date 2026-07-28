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

        course.UpdateDescription(command.Description);

        if (command.Difficulty is not null)
            course.ChangeDifficulty(Enum.Parse<Difficulty>(command.Difficulty, ignoreCase: true));

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


    private static LineString ResolveRoute(GeoJsonLineStringDto? geoJson, string? gpxXml)
    {
        if (geoJson is not null)
        {
            var coords = geoJson.Coordinates.Select(c => new Coordinate(c[0], c[1])).ToArray();
            return new LineString(coords) { SRID = 4326 };
        }

        if (gpxXml is not null)
            return GpxParser.Parse(gpxXml);

        // Handler 側で「Route か GpxXml のいずれかが非 null」を確認してから呼んでいるため到達しない。
        // ここに来たら入力ではなく呼び出し側の構造が壊れている。
        throw new InvalidOperationException(
            "ResolveRoute は route または gpxXml のいずれかが指定された状態でのみ呼ばれるはずです");
    }
}