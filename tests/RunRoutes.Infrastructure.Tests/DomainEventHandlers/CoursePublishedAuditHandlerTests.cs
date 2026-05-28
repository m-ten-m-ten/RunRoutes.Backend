using System.Text.Json;
using Moq;
using RunRoutes.Core.Common;
using RunRoutes.Infrastructure.DomainEventHandlers;
using RunRoutes.Infrastructure.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Courses.Events;

namespace RunRoutes.Infrastructure.Tests.DomainEventHandlers;

[Collection("Database")]
public class CoursePublishedAuditHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_AuditLogが正しく保存される()
    {
        // Arrange
        await _fixture.ResetAsync();

        var actorId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var courseOwnerId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;
        var domainEvent = new CoursePublishedEvent(courseId, courseOwnerId, occurredAt);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns(actorId);

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new CoursePublishedAuditHandler(db, currentUserMock.Object);
            await handler.HandleAsync(domainEvent, default);
            await db.SaveChangesAsync();
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var saved = await db.AuditLogs.SingleAsync();

            Assert.Equal(nameof(CoursePublishedEvent), saved.EventType);
            Assert.Equal(actorId, saved.ActorId);
            Assert.Equal("Course", saved.TargetType);
            Assert.Equal(courseId, saved.TargetId);
            Assert.Equal(occurredAt, saved.OccurredAt, TimeSpan.FromMicroseconds(1));

            using var doc = JsonDocument.Parse(saved.Payload);
            var courseIdInPayload = doc.RootElement.GetProperty("courseId").GetGuid();
            var courseOwnerIdInPayload = doc.RootElement.GetProperty("ownerId").GetGuid();
            Assert.Equal(courseId, courseIdInPayload);
            Assert.Equal(courseOwnerId, courseOwnerIdInPayload);
        }
    }

    [Fact]
    public async Task HandleAsync_ActorIdがnullでも保存できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        var courseId = Guid.NewGuid();
        var courseOwnerId = Guid.NewGuid();
        var domainEvent = new CoursePublishedEvent(courseId, courseOwnerId, DateTime.UtcNow);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns((Guid?)null);

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new CoursePublishedAuditHandler(db, currentUserMock.Object);
            await handler.HandleAsync(domainEvent, default);
            await db.SaveChangesAsync();
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var saved = await db.AuditLogs.SingleAsync();
            Assert.Null(saved.ActorId);
            Assert.Equal(courseId, saved.TargetId);
        }
    }
}