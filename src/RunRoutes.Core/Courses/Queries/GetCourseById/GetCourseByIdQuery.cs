using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Courses.Dtos;

namespace RunRoutes.Core.Courses.Queries.GetCourseById;

public record GetCourseByIdQuery(Guid CourseId, Guid? CurrentUserId)
    : IQuery<GetCourseResponse>;