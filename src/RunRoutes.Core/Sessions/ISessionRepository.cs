namespace RunRoutes.Core.Sessions;

public interface ISessionRepository
{
    Task AddAsync(Session session);
    Task<Session?> GetByRefreshTokenForUpdateAsync(string refreshTokenValue);
    Task UpdateAsync(Session session);
    Task DeleteAsync(Session session);
}