using RunRoutes.Core.Entities;

namespace RunRoutes.Core.Interfaces.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
