using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Tags;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Repositories;

public class TagRepository(AppDbContext db) : ITagRepository
{
    public async Task<IEnumerable<Tag>> GetAllAsync() =>
        await db.Tags.AsNoTracking().OrderBy(t => t.Name).ToListAsync();

    public Task<Tag?> GetByIdAsync(Guid id) =>
        db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

    public Task<Tag?> GetByIdForUpdateAsync(Guid id) =>
        db.Tags.FirstOrDefaultAsync(t => t.Id == id);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null) =>
        excludeId is null
            ? db.Tags.AnyAsync(t => t.Name == name)
            : db.Tags.AnyAsync(t => t.Name == name && t.Id != excludeId);

    public Task<bool> HasCoursesAsync(Guid id) =>
        db.Tags.AnyAsync(t => t.Id == id && t.Courses.Any());

    public async Task AddAsync(Tag tag)
    {
        db.Tags.Add(tag);
        await db.SaveChangesAsync();
    }

    public async Task UpdateWithConcurrencyCheckAsync(Tag tag, uint expectedVersion)
    {
        db.Entry(tag).OriginalValues[nameof(Tag.Version)] = expectedVersion;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("他のユーザーが先にこのタグを更新しました。最新の内容を再取得してください。", ErrorCodes.TagRowVersionMismatch);
        }
    }

    public async Task DeleteWithConcurrencyCheckAsync(Tag tag, uint expectedVersion)
    {
        db.Tags.Remove(tag);
        db.Entry(tag).OriginalValues[nameof(Tag.Version)] = expectedVersion;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("他のユーザーが先にこのタグを更新しました。最新の内容を再取得してください。", ErrorCodes.TagRowVersionMismatch);
        }
    }
}
