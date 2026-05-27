using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Moq;
using RunRoutes.Core.Audit;
using RunRoutes.Core.Common;
using RunRoutes.Core.Users.Events;
using RunRoutes.Infrastructure.DomainEventHandlers;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.DomainEventHandlers;

[Collection("Database")]
public class UserRemovedAuditHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_AuditLogが正しく保存される()
    {
        // Arrange
        await _fixture.ResetAsync();

        var actorId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;
        var domainEvent = new UserRemovedEvent(targetUserId, occurredAt);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns(actorId);

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new UserRemovedAuditHandler(db, currentUserMock.Object);
            await handler.HandleAsync(domainEvent, default);
            await db.SaveChangesAsync();
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var saved = await db.AuditLogs.SingleAsync();

            Assert.Equal(nameof(UserRemovedEvent), saved.EventType);
            Assert.Equal(actorId, saved.ActorId);
            Assert.Equal("User", saved.TargetType);
            Assert.Equal(targetUserId, saved.TargetId);
            Assert.Equal(occurredAt, saved.OccurredAt, TimeSpan.FromMicroseconds(1));

            // payload は JSON としてパースして検証
            using var doc = JsonDocument.Parse(saved.Payload);
            var userIdInPayload = doc.RootElement.GetProperty("userId").GetGuid();
            Assert.Equal(targetUserId, userIdInPayload);
        }
    }

    [Fact]
    public async Task HandleAsync_ActorIdがnullでも保存できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        var targetUserId = Guid.NewGuid();
        var domainEvent = new UserRemovedEvent(targetUserId, DateTime.UtcNow);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns((Guid?)null);

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new UserRemovedAuditHandler(db, currentUserMock.Object);
            await handler.HandleAsync(domainEvent, default);
            await db.SaveChangesAsync();
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var saved = await db.AuditLogs.SingleAsync();
            Assert.Null(saved.ActorId);
            Assert.Equal(targetUserId, saved.TargetId);
        }
    }

    [Fact]
    public async Task HandleAsync_SaveChangeAsyncを呼ばない()
    {
        // Arrange
        await _fixture.ResetAsync();

        var domainEvent = new UserRemovedEvent(Guid.NewGuid(), DateTime.UtcNow);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        // Act
        await using var db = _fixture.CreateDbContext();
        var handler = new UserRemovedAuditHandler(db, currentUserMock.Object);
        await handler.HandleAsync(domainEvent, default);

        // Assert
        // Handler 自身は SaveChangesAsync を呼ばないので、
        // ChangeTracker に Added 状態のエンティティが乗っているだけ
        var tracked = db.ChangeTracker.Entries<AuditLogEntry>().Single();
        Assert.Equal(EntityState.Added, tracked.State);

        // 別 DbContext で DB を見ると、まだ何も保存されていない
        await using var verifyDb = _fixture.CreateDbContext();
        Assert.Equal(0, await verifyDb.AuditLogs.CountAsync());
    }
}