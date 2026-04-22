using RunRoutes.Core.Entities;

namespace RunRoutes.Core.Interfaces.Repositories;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetByCourseIdAsync(Guid courseId);
    Task<int> GetCountByCourseIdAsync(Guid courseId);
    Task<Comment?> GetByIdForUpdateAsync(Guid id);
    Task AddAsync(Comment comment);
    Task UpdateAsync(Comment comment);
    Task DeleteAsync(Comment comment);
}
