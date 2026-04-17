using RunRoutes.Core.Entities;

namespace RunRoutes.Core.Interfaces.Repositories;

public interface ITagRepository
{
    Task<IEnumerable<Tag>> GetAllAsync();
}
