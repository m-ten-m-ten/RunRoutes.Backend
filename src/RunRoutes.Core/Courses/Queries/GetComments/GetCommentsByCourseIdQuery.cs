using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Courses.Dtos;

namespace RunRoutes.Core.Courses.Queries.GetComments;

public record GetCommentsByCourseIdQuery(Guid CourseId) : IQuery<GetCommentsResponse>;