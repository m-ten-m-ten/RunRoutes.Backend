using Microsoft.Extensions.Options;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Users;

public class AuthService(
    IUserRepository userRepository,
    IJwtService jwtService,
    IEmailService emailService,
    IPasswordHasher passwordHasher,
    IOptions<JwtSettings> jwtSettings) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
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

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash.Value))
            throw new ValidationException("メールアドレスまたはパスワードが正しくありません");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return (new LoginResponse(accessToken, ToUserDto(user)), refreshToken);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var user = await _userRepository.GetByRefreshTokenForUpdateAsync(refreshToken);
        if (user is null) return;

        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }

    public async Task<(RefreshResponse Response, string NewRefreshToken)> RefreshAsync(string refreshToken)
    {
        var user = await _userRepository.GetByRefreshTokenForUpdateAsync(refreshToken)
            ?? throw new ValidationException("リフレッシュトークンが無効です");

        if (user.RefreshTokenExpiresAt < DateTime.UtcNow)
            throw new ValidationException("リフレッシュトークンの有効期限が切れています");

        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return (new RefreshResponse(newAccessToken, ToUserDto(user)), newRefreshToken);
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
            if (request.Username != user.Username.Value && await _userRepository.ExistsByUsernameAsync(request.Username))
                throw new ConflictException("このユーザー名はすでに使用されています");
            user.Username = Username.Create(request.Username);
        }

        if (request.NewPassword is not null)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword))
                throw new ValidationException("現在のパスワードを入力してください");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash.Value))
                throw new ValidationException("現在のパスワードが正しくありません");

            user.PasswordHash = HashedPassword.FromHash(BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
        }

        user.UpdatedAt = DateTime.UtcNow;
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
