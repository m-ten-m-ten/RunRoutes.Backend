using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;

namespace RunRoutes.Core.Tags.Commands.DeleteTag;

public record DeleteTagCommand(Guid Id, uint RowVersion) : ICommand<Unit>;