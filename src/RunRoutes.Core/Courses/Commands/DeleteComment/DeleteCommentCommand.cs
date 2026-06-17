using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;

namespace RunRoutes.Core.Courses.Commands.DeleteComment;

public record DeleteCommentCommand(
    Guid CourseId, Guid CommentId, Guid UserId
) : ICommand<Unit>;