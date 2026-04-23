namespace RunRoutes.Core.DTOs.Tags;

public record TagSummaryDto(Guid Id, string Name, uint RowVersion);

public record CreateTagRequest(string Name);
public record UpdateTagRequest(string Name, uint RowVersion);

public record CreateTagResponse(TagSummaryDto Tag);
public record UpdateTagResponse(TagSummaryDto Tag);
