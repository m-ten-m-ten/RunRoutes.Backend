using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Auth.Commands.Refresh;

public record RefreshCommand(string RefreshToken) : ICommand<RefreshResult>;

public record RefreshResult(RefreshResponse Response, string NewRefreshToken);
