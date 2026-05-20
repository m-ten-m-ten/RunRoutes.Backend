using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Users;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task ActivateAsync(string token);
    Task<(LoginResponse Response, string RefreshToken)> LoginAsync(LoginRequest request);
    Task LogoutAsync(string refreshToken);
    Task<(RefreshResponse Response, string NewRefreshToken)> RefreshAsync(string refreshToken);
    Task<MeResponse> GetMeAsync(Guid userId);
    Task<UpdateMeResponse> UpdateMeAsync(Guid userId, UpdateMeRequest request);
    Task<UpdateEmailResponse> UpdateEmailAsync(Guid userId, UpdateEmailRequest request);
    Task ActivateEmailAsync(string token);
}
