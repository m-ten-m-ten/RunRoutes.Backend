using Microsoft.Extensions.Options;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Sessions;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Users;

public class AuthService(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IJwtService jwtService,
    IEmailService emailService,
    IPasswordHasher passwordHasher,
    IOptions<JwtSettings> jwtSettings) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IEmailService _emailService = emailService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
            throw new ConflictException("このメールアドレスはすでに使用されています");
        if (await _userRepository.ExistsByUsernameAsync(request.Username))
            throw new ConflictException("このユーザー名はすでに使用されています");

        var user = User.Register(
            EmailAddress.Create(request.Email),
            Username.Create(request.Username),
            PlainPassword.Create(request.Password),
            _passwordHasher,
            DateTime.UtcNow,
            TimeSpan.FromHours(24));

        await _userRepository.AddAsync(user);
        await _emailService.SendActivationEmailAsync(user.Email.Value, user.Activation!.Value);

        return new RegisterResponse("確認メールを送信しました");
    }

    public async Task ActivateAsync(string token)
    {
        var user = await _userRepository.GetByActivationTokenForUpdateAsync(token)
            ?? throw new NotFoundException("有効化トークンが無効です");

        user.Activate(DateTime.UtcNow);
        await _userRepository.UpdateAsync(user);
    }

    public async Task<(LoginResponse Response, string RefreshToken)> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailForUpdateAsync(request.Email)
            ?? throw new ValidationException("メールアドレスまたはパスワードが正しくありません");

        if (!user.IsActive)
            throw new ValidationException("アカウントが有効化されていません");

        if (!user.VerifyPassword(PlainPassword.Create(request.Password), _passwordHasher))
            throw new ValidationException("メールアドレスまたはパスワードが正しくありません");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var now = DateTime.UtcNow;
        var refreshToken = RefreshToken.Generate(
            now, TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays));
        var session = Session.Start(user.Id, refreshToken, now);

        await _sessionRepository.AddAsync(session);

        return (new LoginResponse(accessToken, ToUserDto(user)), refreshToken.Value);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var session = await _sessionRepository.GetByRefreshTokenForUpdateAsync(refreshToken);
        if (session is null) return;

        await _sessionRepository.DeleteAsync(session);
    }

    public async Task<(RefreshResponse Response, string NewRefreshToken)> RefreshAsync(string refreshToken)
    {
        var session = await _sessionRepository.GetByRefreshTokenForUpdateAsync(refreshToken)
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

        return (new RefreshResponse(newAccessToken, ToUserDto(user)), newRefreshToken.Value);
    }

    public async Task<MeResponse> GetMeAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("ユーザーが見つかりません");

        return new MeResponse(ToUserDto(user));
    }

    public async Task<UpdateMeResponse> UpdateMeAsync(Guid userId, UpdateMeRequest request)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId)
            ?? throw new NotFoundException("ユーザーが見つかりません");

        if (request.Username is not null)
        {
            var newUsername = Username.Create(request.Username);
            if (!user.Username.Equals(newUsername) &&
                await _userRepository.ExistsByUsernameAsync(newUsername.Value))
                throw new ConflictException("このユーザー名はすでに使用されています");
            user.ChangeUsername(newUsername, DateTime.UtcNow);
        }

        if (request.NewPassword is not null)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword))
                throw new ValidationException("現在のパスワードを入力してください");

            user.ChangePassword(
            PlainPassword.Create(request.CurrentPassword),
            PlainPassword.Create(request.NewPassword),
            _passwordHasher,
            DateTime.UtcNow);
        }

        await _userRepository.UpdateAsync(user);
        return new UpdateMeResponse(ToUserDto(user));
    }

    public async Task<UpdateEmailResponse> UpdateEmailAsync(Guid userId, UpdateEmailRequest request)
    {
        var user = await _userRepository.GetByIdForUpdateAsync(userId)
            ?? throw new NotFoundException("ユーザーが見つかりません");

        var newEmail = EmailAddress.Create(request.NewEmail);
        if (await _userRepository.ExistsByEmailAsync(newEmail.Value))
            throw new ConflictException("このメールアドレスはすでに使用されています");

        user.RequestEmailChange(
            newEmail,
            PlainPassword.Create(request.CurrentPassword),
            _passwordHasher,
            DateTime.UtcNow,
            TimeSpan.FromHours(24));

        await _userRepository.UpdateAsync(user);
        await _emailService.SendEmailChangeEmailAsync(newEmail.Value, user.EmailChange!.Token);

        return new UpdateEmailResponse("新しいメールアドレスに確認メールを送信しました");
    }

    public async Task ActivateEmailAsync(string token)
    {
        var user = await _userRepository.GetByEmailChangeTokenForUpdateAsync(token)
            ?? throw new NotFoundException("メール変更トークンが無効です");

        user.ConfirmEmailChange(token, DateTime.UtcNow);
        await _userRepository.UpdateAsync(user);
    }

    private static UserDto ToUserDto(User user) =>
        new(user.Id, user.Email.Value, user.Username.Value, user.Role.ToString(), user.CreatedAt);
}
