using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Tags.Dtos;

namespace RunRoutes.Core.Tags.Commands.CreateTag;

public record CreateTagCommand(string Name) : ICommand<TagSummaryDto>;