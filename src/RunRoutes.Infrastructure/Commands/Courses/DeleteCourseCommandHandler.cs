using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.DeleteCourse;

namespace RunRoutes.Infrastructure.Commands.Courses;

public class DeleteCourseCommandHandler(ICourseRepository courseRepository)
    : ICommandHandler<DeleteCourseCommand, Unit>
{
    private readonly ICourseRepository _courseRepository = courseRepository;

    public async Task<Unit> HandleAsync(
        DeleteCourseCommand command,
        CancellationToken cancellationToken
    )
    {
        var course = await _courseRepository.GetByIdForUpdateAsync(command.Id)
            ?? throw new NotFoundException("コースが見つかりません");

        if (course.UserId != command.UserId)
            throw new ForbiddenException("このコースを削除する権限がありません");

        await _courseRepository.DeleteAsync(course);
        return Unit.Value;
    }
}