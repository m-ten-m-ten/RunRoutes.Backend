using RunRoutes.Core.Common.Commands;

namespace RunRoutes.Core.Courses.Commands.CreateComment;

public record CreateCommentCommand(
    Guid CourseId, Guid UserId, string Body
) : ICommand<Guid>;