using RunRoutes.Core.Auth.Commands.Activate;
using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Users;

namespace RunRoutes.Infrastructure.Commands.Auth;

public class ActivateCommandHandler(
    IUserRepository userRepository
) : ICommandHandler<ActivateCommand, Unit>
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<Unit> HandleAsync(
        ActivateCommand command,
        CancellationToken cancellationToken
    )
    {
        var user = await _userRepository.GetByActivationTokenForUpdateAsync(command.ActivationToken)
            ?? throw new NotFoundException("有効化トークンが無効です");

        user.Activate(DateTime.UtcNow);
        await _userRepository.UpdateAsync(user);

        return Unit.Value;
    }
}