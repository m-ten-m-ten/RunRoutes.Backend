using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;

namespace RunRoutes.Core.Courses.Commands.DeleteCourse;

public record DeleteCourseCommand(Guid Id, Guid UserId) : ICommand<Unit>;