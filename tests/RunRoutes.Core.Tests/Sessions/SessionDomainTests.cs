using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Sessions;
using RunRoutes.Core.Sessions.Events;

namespace RunRoutes.Core.Tests.Sessions;

public class SessionDomainTests
{
    private static RefreshToken ValidToken(DateTime now) =>
        RefreshToken.Generate(now, TimeSpan.FromDays(7));

    // ========================================
    // Session.Start
    // ========================================

    [Fact]
    public void Start_正常にセッションが生成される()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var token = ValidToken(now);

        var session = Session.Start(userId, token, now);

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(token, session.RefreshToken);
        Assert.Equal(now, session.CreatedAt);
    }

    [Fact]
    public void Start_SessionStartedEventが発火する()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();

        var session = Session.Start(userId, ValidToken(now), now);

        var evt = Assert.Single(session.DomainEvents);
        var started = Assert.IsType<SessionStartedEvent>(evt);
        Assert.Equal(session.Id, started.SessionId);
        Assert.Equal(userId, started.UserId);
        Assert.Equal(now, started.OccurredAt);
    }

    // ========================================
    // Session.Rotate
    // ========================================

    [Fact]
    public void Rotate_正常にトークンが更新される()
    {
        var now = DateTime.UtcNow;
        var session = Session.Start(Guid.NewGuid(), ValidToken(now), now);
        var newToken = RefreshToken.Generate(now, TimeSpan.FromDays(7));

        session.Rotate(newToken, now);

        Assert.Equal(newToken, session.RefreshToken);
    }

    [Fact]
    public void Rotate_期限切れトークンでValidationException()
    {
        var now = DateTime.UtcNow;
        var expiredToken = RefreshToken.FromStorage("expired", now.AddDays(-1));
        var session = Session.Start(Guid.NewGuid(), expiredToken, now.AddDays(-8));

        Assert.Throws<ValidationException>(() =>
            session.Rotate(ValidToken(now), now));
    }

    // ========================================
    // Session.Revoke
    // ========================================

    [Fact]
    public void Revoke_SessionRevokedEventが発火する()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var session = Session.Start(userId, ValidToken(now), now);
        session.ClearDomainEvents(); // Start のイベントを除外

        session.Revoke(now);

        var evt = Assert.Single(session.DomainEvents);
        var revoked = Assert.IsType<SessionRevokedEvent>(evt);
        Assert.Equal(session.Id, revoked.SessionId);
        Assert.Equal(userId, revoked.UserId);
        Assert.Equal(now, revoked.OccurredAt);
    }

    // ========================================
    // Session.IsExpired
    // ========================================

    [Fact]
    public void IsExpired_期限前はfalse()
    {
        var now = DateTime.UtcNow;
        var session = Session.Start(Guid.NewGuid(), ValidToken(now), now);

        Assert.False(session.IsExpired(now));
        Assert.False(session.IsExpired(now.AddDays(6)));
    }

    [Fact]
    public void IsExpired_期限到達でtrue()
    {
        var now = DateTime.UtcNow;
        var session = Session.Start(Guid.NewGuid(), ValidToken(now), now);

        Assert.True(session.IsExpired(now.AddDays(7)));
    }
}
