using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Auth.Commands.UpdateEmail;

public record UpdateEmailCommand(Guid UserId, string NewEmail, string CurrentPassword) : ICommand<UpdateEmailResponse>;