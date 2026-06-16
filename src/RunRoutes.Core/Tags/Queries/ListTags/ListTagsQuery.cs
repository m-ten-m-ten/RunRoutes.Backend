using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Tags.Dtos;

namespace RunRoutes.Core.Tags.Queries.ListTags;

public record ListTagsQuery() : IQuery<IEnumerable<TagSummaryDto>>;