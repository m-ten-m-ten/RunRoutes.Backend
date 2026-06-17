using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.CreateComment;

namespace RunRoutes.Infrastructure.Commands.Courses;

public class CreateCommentCommandHandler(ICourseRepository courseRepository)
    : ICommandHandler<CreateCommentCommand, Guid>
{
    private readonly ICourseRepository _courseRepository = courseRepository;

    public async Task<Guid> HandleAsync(
        CreateCommentCommand command,
        CancellationToken cancellationToken
    )
    {
        var course = await _courseRepository.GetByIdForUpdateAsync(command.CourseId)
            ?? throw new NotFoundException("コースが見つかりません");

        var comment = course.AddComment(command.UserId, command.Body);
        await _courseRepository.UpdateAsync(course);
        return comment.Id;
    }
}