using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;

namespace RunRoutes.Core.Auth.Commands.ActivateEmail;

public record ActivateEmailCommand(string EmailChangeToken) : ICommand<Unit>;
