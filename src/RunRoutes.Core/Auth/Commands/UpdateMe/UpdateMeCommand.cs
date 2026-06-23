using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Auth.Commands.UpdateMe;

public record UpdateMeCommand(Guid UserId, string? Username, string? CurrentPassword, string? NewPassword) : ICommand<UpdateMeResponse>;