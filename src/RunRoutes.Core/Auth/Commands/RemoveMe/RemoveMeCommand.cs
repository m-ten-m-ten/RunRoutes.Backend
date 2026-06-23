using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Auth.Commands.RemoveMe;

public record RemoveMeCommand(Guid UserId) : ICommand<DeleteAccountResponse>;