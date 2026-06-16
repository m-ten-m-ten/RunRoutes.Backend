using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Tags.Dtos;

namespace RunRoutes.Core.Tags.Commands.UpdateTag;

public record UpdateTagCommand(Guid Id, string Name, uint RowVersion) : ICommand<TagSummaryDto>;