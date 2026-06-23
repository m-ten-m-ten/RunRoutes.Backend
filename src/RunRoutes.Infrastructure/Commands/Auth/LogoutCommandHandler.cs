using RunRoutes.Core.Auth.Commands.Logout;
using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Sessions;

namespace RunRoutes.Infrastructure.Commands.Auth;

public class LogoutCommandHandler(
    ISessionRepository sessionRepository
) : ICommandHandler<LogoutCommand, Unit>
{
    private readonly ISessionRepository _sessionRepository = sessionRepository;

    public async Task<Unit> HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken
    )
    {
        var session = await _sessionRepository.GetByRefreshTokenForUpdateAsync(command.RefreshToken);
        if (session is null) return Unit.Value;

        await _sessionRepository.DeleteAsync(session);
        return Unit.Value;
    }
}