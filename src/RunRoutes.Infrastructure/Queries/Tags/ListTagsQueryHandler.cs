using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Tags.Queries.ListTags;
using RunRoutes.Core.Tags.Dtos;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Queries.Tags;

public class ListTagsQueryHandler(AppDbContext db) : IQueryHandler<ListTagsQuery, IEnumerable<TagSummaryDto>>
{
    private readonly AppDbContext _db = db;

    public async Task<IEnumerable<TagSummaryDto>> HandleAsync(ListTagsQuery query, CancellationToken cancellationToken)
    {
        var tags = await _db.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TagSummaryDto(
                t.Id,
                t.Name,
                t.Version
            ))
            .ToListAsync(cancellationToken);

        return tags;
    }
}