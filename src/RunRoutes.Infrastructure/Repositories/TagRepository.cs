using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Entities;
using RunRoutes.Core.Interfaces.Repositories;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Repositories;

public class TagRepository(AppDbContext db) : ITagRepository
{
    public async Task<IEnumerable<Tag>> GetAllAsync() =>
        await db.Tags.OrderBy(t => t.Name).ToListAsync();
}
