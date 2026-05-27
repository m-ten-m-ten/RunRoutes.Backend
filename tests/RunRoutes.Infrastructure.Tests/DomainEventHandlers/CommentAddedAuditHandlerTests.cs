using System.Text.Json;
using Moq;
using RunRoutes.Core.Common;
using RunRoutes.Infrastructure.DomainEventHandlers;
using RunRoutes.Infrastructure.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Courses.Events;

namespace RunRoutes.Infrastructure.Tests.DomainEventHandlers;

[Collection("Database")]
public class CommentAddedAuditHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_AuditLogが正しく保存される()
    {
        // Arrange
        await _fixture.ResetAsync();

        var actorId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var commentAuthorId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;
        var domainEvent = new CommentAddedEvent(commentId, courseId, commentAuthorId, occurredAt);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns(actorId);

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new CommentAddedAuditHandler(db, currentUserMock.Object);
            await handler.HandleAsync(domainEvent, default);
            await db.SaveChangesAsync();
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var saved = await db.AuditLogs.SingleAsync();

            Assert.Equal(nameof(CommentAddedEvent), saved.EventType);
            Assert.Equal(actorId, saved.ActorId);
            Assert.Equal("Comment", saved.TargetType);
            Assert.Equal(commentId, saved.TargetId);
            Assert.Equal(occurredAt, saved.OccurredAt, TimeSpan.FromMicroseconds(1));

            using var doc = JsonDocument.Parse(saved.Payload);
            var commentIdInPayload = doc.RootElement.GetProperty("commentId").GetGuid();
            var courseIdInPayload = doc.RootElement.GetProperty("courseId").GetGuid();
            var commentAuthorIdInPayload = doc.RootElement.GetProperty("authorId").GetGuid();
            Assert.Equal(commentId, commentIdInPayload);
            Assert.Equal(courseId, courseIdInPayload);
            Assert.Equal(commentAuthorId, commentAuthorIdInPayload);
        }
    }

    [Fact]
    public async Task HandleAsync_ActorIdがnullでも保存できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        var commentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var commentAuthorId = Guid.NewGuid();
        var domainEvent = new CommentAddedEvent(commentId, courseId, commentAuthorId, DateTime.UtcNow);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns((Guid?)null);

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new CommentAddedAuditHandler(db, currentUserMock.Object);
            await handler.HandleAsync(domainEvent, default);
            await db.SaveChangesAsync();
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var saved = await db.AuditLogs.SingleAsync();
            Assert.Null(saved.ActorId);
            Assert.Equal(commentId, saved.TargetId);
        }
    }
}