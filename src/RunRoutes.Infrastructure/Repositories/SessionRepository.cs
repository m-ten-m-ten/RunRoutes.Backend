using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Sessions;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Repositories;

public class SessionRepository(AppDbContext db) : ISessionRepository
{
    public async Task AddAsync(Session session)
    {
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
    }

    public async Task<Session?> GetByRefreshTokenForUpdateAsync(string refreshTokenValue)
    {
        return await db.Sessions
            .FromSqlInterpolated($@"
                SELECT * FROM sessions
                WHERE refresh_token = {refreshTokenValue}
                FOR UPDATE")
            .SingleOrDefaultAsync();
    }

    public async Task UpdateAsync(Session session)
    {
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Session session)
    {
        // SessionRevokedEvent を発火させてから削除
        session.Revoke(DateTime.UtcNow);
        db.Sessions.Remove(session);
        await db.SaveChangesAsync();
        // ※ AddDomainEvent → ChangeTracker → SaveChangesAsync の流れで監査ログハンドラに渡る
    }
}