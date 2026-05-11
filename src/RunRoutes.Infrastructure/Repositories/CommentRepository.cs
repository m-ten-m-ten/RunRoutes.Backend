using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Courses;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Repositories;

public class CommentRepository(AppDbContext db) : ICommentRepository
{
    public async Task<IEnumerable<Comment>> GetByCourseIdAsync(Guid courseId) =>
        await db.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.CourseId == courseId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

    public Task<int> GetCountByCourseIdAsync(Guid courseId) =>
        db.Comments.CountAsync(c => c.CourseId == courseId);

    public Task<Comment?> GetByIdForUpdateAsync(Guid id) =>
        db.Comments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(Comment comment)
    {
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Comment comment)
    {
        db.Comments.Update(comment);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Comment comment)
    {
        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
    }
}
