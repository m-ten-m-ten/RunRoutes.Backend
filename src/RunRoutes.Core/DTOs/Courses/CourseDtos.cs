using RunRoutes.Core.DTOs.Common;

namespace RunRoutes.Core.DTOs.Courses;

public record GetCoursesQuery(double? Lat, double? Lng, double? RadiusKm, string? Difficulty, IEnumerable<Guid>? TagIds, int Page = 1, int PageSize = 20);
public record GetCoursesResponse(IEnumerable<CourseListItemDto> Courses, int TotalCount, int Page, int PageSize);
public record CourseListItemDto(Guid Id, string Title, string Difficulty, double DistanceM, bool IsPublic, UserDto User, IEnumerable<TagDto> Tags, int CommentCount, DateTime CreatedAt);
public record CourseDetailDto(Guid Id, string Title, string? Description, string Difficulty, double DistanceM, bool IsPublic, GeoJsonLineStringDto Route, UserDto User, IEnumerable<TagDto> Tags, int CommentCount, DateTime CreatedAt, DateTime UpdatedAt);
public record GetCourseResponse(CourseDetailDto Course);
public record CreateCourseRequest(string Title, string? Description, string Difficulty, bool IsPublic, GeoJsonLineStringDto? Route, string? GpxXml, IEnumerable<Guid> TagIds);
public record CreateCourseResponse(CourseDetailDto Course);
public record UpdateCourseRequest(string? Title, string? Description, string? Difficulty, bool? IsPublic, GeoJsonLineStringDto? Route, string? GpxXml, IEnumerable<Guid>? TagIds);
public record UpdateCourseResponse(CourseDetailDto Course);
