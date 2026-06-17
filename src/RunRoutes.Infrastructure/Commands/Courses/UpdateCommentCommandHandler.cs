using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.UpdateComment;

namespace RunRoutes.Infrastructure.Commands.Courses;

public class UpdateCommentCommandHandler(ICourseRepository courseRepository)
    : ICommandHandler<UpdateCommentCommand, Guid>
{
    private readonly ICourseRepository _courseRepository = courseRepository;

    public async Task<Guid> HandleAsync(
        UpdateCommentCommand command,
        CancellationToken cancellationToken
    )
    {
        var course = await _courseRepository.GetByIdForUpdateAsync(command.CourseId)
            ?? throw new NotFoundException("コースが見つかりません");

        var comment = course.EditComment(command.CommentId, command.UserId, command.Body);
        await _courseRepository.UpdateAsync(course);
        return comment.Id;
    }
}