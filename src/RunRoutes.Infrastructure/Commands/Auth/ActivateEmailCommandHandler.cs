using RunRoutes.Core.Auth.Commands.ActivateEmail;
using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;

namespace RunRoutes.Infrastructure.Commands.Auth;

public class ActivateEmailCommandHandler(
    IUserRepository userRepository
) : ICommandHandler<ActivateEmailCommand, Unit>
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<Unit> HandleAsync(
        ActivateEmailCommand command,
        CancellationToken cancellationToken
    )
    {
        var user = await _userRepository.GetByEmailChangeTokenForUpdateAsync(command.EmailChangeToken)
            ?? throw new NotFoundException("メール変更トークンが無効です");

        user.ConfirmEmailChange(command.EmailChangeToken, DateTime.UtcNow);
        await _userRepository.UpdateAsync(user);

        return Unit.Value;
    }
}