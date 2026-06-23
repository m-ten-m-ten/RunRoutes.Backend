using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Auth.Commands.RegisterUser;

public record RegisterUserCommand(string Email, string Username, string Password) : ICommand<RegisterResponse>;