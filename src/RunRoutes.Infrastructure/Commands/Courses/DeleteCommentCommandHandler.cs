using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.DeleteComment;

namespace RunRoutes.Infrastructure.Commands.Courses;

public class DeleteCommentCommandHandler(ICourseRepository courseRepository)
    : ICommandHandler<DeleteCommentCommand, Unit>
{
    private readonly ICourseRepository _courseRepository = courseRepository;

    public async Task<Unit> HandleAsync(
        DeleteCommentCommand command,
        CancellationToken cancellationToken
    )
    {
        var course = await _courseRepository.GetByIdForUpdateAsync(command.CourseId)
            ?? throw new NotFoundException("コースが見つかりません");

        course.RemoveComment(command.CommentId, command.UserId);
        await _courseRepository.UpdateAsync(course);

        return Unit.Value;
    }
}