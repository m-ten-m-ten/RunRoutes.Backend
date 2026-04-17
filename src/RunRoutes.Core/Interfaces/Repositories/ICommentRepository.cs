using RunRoutes.Core.Entities;

namespace RunRoutes.Core.Interfaces.Repositories;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetByCourseIdAsync(Guid courseId);
    Task<Comment?> GetByIdAsync(Guid id);
    Task AddAsync(Comment comment);
    Task UpdateAsync(Comment comment);
    Task DeleteAsync(Comment comment);
}
