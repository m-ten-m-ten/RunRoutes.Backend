using System.Text.Json;
using Moq;
using RunRoutes.Core.Common;
using RunRoutes.Core.Sessions.Events;
using RunRoutes.Infrastructure.DomainEventHandlers;
using RunRoutes.Infrastructure.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace RunRoutes.Infrastructure.Tests.DomainEventHandlers;

[Collection("Database")]
public class SessionRevokedAuditHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_AuditLogが正しく保存される()
    {
        // Arrange
        await _fixture.ResetAsync();

        var actorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;
        var domainEvent = new SessionRevokedEvent(sessionId, userId, occurredAt);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns(actorId);

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new SessionRevokedAuditHandler(db, currentUserMock.Object);
            await handler.HandleAsync(domainEvent, default);
            await db.SaveChangesAsync();
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var saved = await db.AuditLogs.SingleAsync();

            Assert.Equal(nameof(SessionRevokedEvent), saved.EventType);
            Assert.Equal(actorId, saved.ActorId);
            Assert.Equal("Session", saved.TargetType);
            Assert.Equal(sessionId, saved.TargetId);
            Assert.Equal(occurredAt, saved.OccurredAt, TimeSpan.FromMicroseconds(1));

            // payload には UserId が含まれている（SessionId とは別物であることを確認）
            using var doc = JsonDocument.Parse(saved.Payload);
            var userIdInPayload = doc.RootElement.GetProperty("userId").GetGuid();
            Assert.Equal(userId, userIdInPayload);
            Assert.NotEqual(sessionId, userIdInPayload);
        }
    }

    [Fact]
    public async Task HandleAsync_ActorIdがnullでも保存できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var domainEvent = new SessionRevokedEvent(sessionId, userId, DateTime.UtcNow);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns((Guid?)null);

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new SessionRevokedAuditHandler(db, currentUserMock.Object);
            await handler.HandleAsync(domainEvent, default);
            await db.SaveChangesAsync();
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var saved = await db.AuditLogs.SingleAsync();
            Assert.Null(saved.ActorId);
            Assert.Equal(sessionId, saved.TargetId);
        }
    }
}