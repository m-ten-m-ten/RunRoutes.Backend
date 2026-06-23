using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;

namespace RunRoutes.Core.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : ICommand<Unit>;
