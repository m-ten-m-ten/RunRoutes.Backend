using RunRoutes.Core.Courses.Dtos;

namespace RunRoutes.Core.Courses;

public interface ICourseService
{
    Task<GetCoursesResponse> GetListAsync(GetCoursesQuery query, Guid? currentUserId);
    Task<GetCourseResponse> GetByIdAsync(Guid id, Guid? currentUserId);
    Task<CreateCourseResponse> CreateAsync(CreateCourseRequest request, Guid userId);
    Task<UpdateCourseResponse> UpdateAsync(Guid id, UpdateCourseRequest request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
