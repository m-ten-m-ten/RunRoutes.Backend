using Microsoft.Extensions.Options;
using RunRoutes.Core.Auth.Commands.Refresh;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Sessions;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Users;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Infrastructure.Commands.Auth;

public class RefreshCommandHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IJwtService jwtService,
    IOptions<JwtSettings> jwtSettings
) : ICommandHandler<RefreshCommand, RefreshResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IJwtService _jwtService = jwtService;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<RefreshResult> HandleAsync(
        RefreshCommand command,
        CancellationToken cancellationToken
    )
    {
        var session = await _sessionRepository.GetByRefreshTokenForUpdateAsync(command.RefreshToken)
            ?? throw new ValidationException("リフレッシュトークンが無効です");

        var now = DateTime.UtcNow;
        if (session.IsExpired(now))
            throw new ValidationException("リフレッシュトークンの有効期限が切れています");

        var user = await _userRepository.GetByIdAsync(session.UserId)
            ?? throw new ValidationException("ユーザーが見つかりません");

        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = RefreshToken.Generate(
            now, TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays));

        session.Rotate(newRefreshToken, now);
        await _sessionRepository.UpdateAsync(session);

        return new RefreshResult(new RefreshResponse(newAccessToken, UserDto.FromUser(user)), newRefreshToken.Value);
    }
}