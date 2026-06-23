using Microsoft.Extensions.Options;
using RunRoutes.Core.Auth.Commands.Login;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Sessions;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Users;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Infrastructure.Commands.Auth;

public class LoginCommandHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IJwtService jwtService,
    IPasswordHasher passwordHasher,
    IOptions<JwtSettings> jwtSettings
) : ICommandHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<LoginResult> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken
    )
    {
        var user = await _userRepository.GetByEmailForUpdateAsync(command.Email)
            ?? throw new ValidationException("メールアドレスまたはパスワードが正しくありません");

        if (!user.IsActive)
            throw new ValidationException("アカウントが有効化されていません");

        if (!user.VerifyPassword(PlainPassword.CreateForVerification(command.Password), _passwordHasher))
            throw new ValidationException("メールアドレスまたはパスワードが正しくありません");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var now = DateTime.UtcNow;
        var refreshToken = RefreshToken.Generate(
            now, TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays));
        var session = Session.Start(user.Id, refreshToken, now);

        await _sessionRepository.AddAsync(session);
        return new LoginResult(new LoginResponse(accessToken, UserDto.FromUser(user)), refreshToken.Value);
    }
}