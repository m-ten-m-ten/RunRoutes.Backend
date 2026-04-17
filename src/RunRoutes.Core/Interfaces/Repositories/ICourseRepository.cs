using RunRoutes.Core.DTOs.Courses;
using RunRoutes.Core.Entities;

namespace RunRoutes.Core.Interfaces.Repositories;

public interface ICourseRepository
{
    Task<(IEnumerable<Course> Courses, int TotalCount)> GetListAsync(GetCoursesQuery query, Guid? currentUserId);
    Task<Course?> GetByIdAsync(Guid id);
    Task AddAsync(Course course);
    Task UpdateAsync(Course course);
    Task DeleteAsync(Course course);
    Task<IEnumerable<Tag>> GetTagsByIdsAsync(IEnumerable<Guid> tagIds);
}
