using NetTopologySuite.Geometries;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Tags.Dtos;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Courses;

public class CourseService(ICourseRepository courseRepository) : ICourseService
{
    private readonly ICourseRepository _courseRepository = courseRepository;

    public async Task<GetCoursesResponse> GetListAsync(GetCoursesQuery query, Guid? currentUserId)
    {
        var (courses, totalCount) = await _courseRepository.GetListAsync(query, currentUserId);
        var items = courses.Select(c => ToCourseListItemDto(c)).ToList();
        return new GetCoursesResponse(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<GetCourseResponse> GetByIdAsync(Guid id, Guid? currentUserId)
    {
        var course = await _courseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("コースが見つかりません");

        if (!course.IsPublic && course.UserId != currentUserId)
            throw new ForbiddenException("このコースにアクセスする権限がありません");

        return new GetCourseResponse(ToCourseDetailDto(course));
    }

    public async Task<CreateCourseResponse> CreateAsync(CreateCourseRequest request, Guid userId)
    {
        var difficulty = ParseDifficulty(request.Difficulty);
        var route = ResolveRoute(request.Route, request.GpxXml);
        var tags = await _courseRepository.GetTagsByIdsForUpdateAsync(request.TagIds ?? []);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Difficulty = difficulty,
            Route = route,
            Distance = Distance.FromMeters(CalculateDistance(route)),
            IsPublic = request.IsPublic,
            Tags = tags.ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _courseRepository.AddAsync(course);
        var created = await _courseRepository.GetByIdAsync(course.Id)
            ?? throw new NotFoundException("コースが見つかりません");
        return new CreateCourseResponse(ToCourseDetailDto(created));
    }

    public async Task<UpdateCourseResponse> UpdateAsync(Guid id, UpdateCourseRequest request, Guid userId)
    {
        var course = await _courseRepository.GetByIdForUpdateAsync(id)
            ?? throw new NotFoundException("コースが見つかりません");

        if (course.UserId != userId)
            throw new ForbiddenException("このコースを編集する権限がありません");

        Difficulty? parsedDifficulty = null;
        if (request.Difficulty is not null)
            parsedDifficulty = ParseDifficulty(request.Difficulty);

        if (request.Title is not null) course.Title = request.Title;
        if (request.Description is not null) course.Description = request.Description;
        if (parsedDifficulty is not null) course.Difficulty = parsedDifficulty.Value;
        if (request.IsPublic is not null) course.IsPublic = request.IsPublic.Value;

        if (request.Route is not null || request.GpxXml is not null)
        {
            var route = ResolveRoute(request.Route, request.GpxXml);
            course.Route = route;
            course.Distance = Distance.FromMeters(CalculateDistance(route));
        }

        if (request.TagIds is not null)
        {
            var tags = await _courseRepository.GetTagsByIdsForUpdateAsync(request.TagIds);
            course.Tags = tags.ToList();
        }

        course.UpdatedAt = DateTime.UtcNow;
        await _courseRepository.UpdateAsync(course);

        var updated = await _courseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("コースが見つかりません");
        return new UpdateCourseResponse(ToCourseDetailDto(updated));
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var course = await _courseRepository.GetByIdForUpdateAsync(id)
            ?? throw new NotFoundException("コースが見つかりません");

        if (course.UserId != userId)
            throw new ForbiddenException("このコースを削除する権限がありません");

        await _courseRepository.DeleteAsync(course);
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

    private static double CalculateDistance(LineString route)
    {
        double total = 0;
        for (int i = 0; i < route.Coordinates.Length - 1; i++)
        {
            total += HaversineDistance(route.Coordinates[i], route.Coordinates[i + 1]);
        }
        return total;
    }

    private static double HaversineDistance(Coordinate a, Coordinate b)
    {
        const double R = 6371000;
        var lat1 = a.Y * Math.PI / 180;
        var lat2 = b.Y * Math.PI / 180;
        var dLat = (b.Y - a.Y) * Math.PI / 180;
        var dLon = (b.X - a.X) * Math.PI / 180;
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }

    private static CourseListItemDto ToCourseListItemDto(Course c) => new(
        c.Id, c.Title, c.Difficulty.ToString().ToLowerInvariant(), c.Distance.Meters, c.IsPublic,
        new UserDto(c.User.Id, c.User.Email, c.User.Username, c.User.Role.ToString(), c.User.CreatedAt),
        c.Tags.Select(t => new TagDto(t.Id, t.Name)),
        c.CommentCount,
        c.CreatedAt
    );

    private static CourseDetailDto ToCourseDetailDto(Course c) => new(
        c.Id, c.Title, c.Description, c.Difficulty.ToString().ToLowerInvariant(), c.Distance.Meters, c.IsPublic,
        new GeoJsonLineStringDto("LineString", c.Route.Coordinates.Select(coord => new[] { coord.X, coord.Y })),
        new UserDto(c.User.Id, c.User.Email, c.User.Username, c.User.Role.ToString(), c.User.CreatedAt),
        c.Tags.Select(t => new TagDto(t.Id, t.Name)),
        c.CommentCount,
        c.CreatedAt,
        c.UpdatedAt
    );
}
