namespace RunRoutes.Core.Tags;

public interface ITagRepository
{
    Task<IEnumerable<Tag>> GetAllAsync();
    Task<Tag?> GetByIdAsync(Guid id);
    Task<Tag?> GetByIdForUpdateAsync(Guid id);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);
    Task<bool> HasCoursesAsync(Guid id);
    Task AddAsync(Tag tag);
    Task UpdateWithConcurrencyCheckAsync(Tag tag, uint expectedVersion);
    Task DeleteWithConcurrencyCheckAsync(Tag tag, uint expectedVersion);
}
