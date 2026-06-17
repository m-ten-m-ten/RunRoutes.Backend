using RunRoutes.Core.Common.Commands;

namespace RunRoutes.Core.Courses.Commands.UpdateComment;

public record UpdateCommentCommand(
    Guid CourseId, Guid CommentId, Guid UserId, string Body
) : ICommand<Guid>;