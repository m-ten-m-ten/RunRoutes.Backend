using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Sessions.Events;

namespace RunRoutes.Core.Sessions;

public class Session : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public RefreshToken RefreshToken { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private Session() { } // EF Core 用

    public static Session Start(Guid userId, RefreshToken refreshToken, DateTime now)
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshToken = refreshToken,
            CreatedAt = now,
        };
        session.AddDomainEvent(new SessionStartedEvent(session.Id, userId, now));
        return session;
    }

    public void Rotate(RefreshToken newToken, DateTime now)
    {
        if (RefreshToken.IsExpired(now))
            throw new ValidationException("リフレッシュトークンの有効期限が切れています");
        RefreshToken = newToken;
        // Rotate ではイベント発火しない(頻度高い & 重要度低い)
    }

    public void Revoke(DateTime now)
    {
        // 物理削除前にイベントを発火する用のヘルパー
        AddDomainEvent(new SessionRevokedEvent(Id, UserId, now));
    }

    public bool IsExpired(DateTime now) => RefreshToken.IsExpired(now);
}