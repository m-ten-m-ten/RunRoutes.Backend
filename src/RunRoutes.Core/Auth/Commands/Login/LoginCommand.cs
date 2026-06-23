using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : ICommand<LoginResult>;

public record LoginResult(LoginResponse Response, string RefreshToken);
