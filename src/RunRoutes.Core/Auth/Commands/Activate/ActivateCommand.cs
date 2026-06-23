using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;

namespace RunRoutes.Core.Auth.Commands.Activate;

public record ActivateCommand(string ActivationToken) : ICommand<Unit>;
