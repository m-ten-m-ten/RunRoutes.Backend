using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Tags;

namespace RunRoutes.Core.Courses;

public interface ICourseRepository
{
    Task<(IEnumerable<Course> Courses, int TotalCount)> GetListAsync(GetCoursesQuery query, Guid? currentUserId);
    Task<Course?> GetByIdAsync(Guid id);
    Task<Course?> GetByIdForUpdateAsync(Guid id);
    Task<bool> ExistsByIdAsync(Guid id);
    Task AddAsync(Course course);
    Task UpdateAsync(Course course);
    Task DeleteAsync(Course course);
    Task<IEnumerable<Tag>> GetTagsByIdsForUpdateAsync(IEnumerable<Guid> tagIds);
}
