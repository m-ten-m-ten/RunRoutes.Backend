using System.Text.Json;
using RunRoutes.Core.Audit;
using RunRoutes.Core.Common;
using RunRoutes.Core.Common.DomainEvents;
using RunRoutes.Core.Courses.Events;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.DomainEventHandlers;

public class CoursePublishedAuditHandler(AppDbContext db, ICurrentUserService currentUser) : IDomainEventHandler<CoursePublishedEvent>
{
    private readonly AppDbContext _db = db;
    private readonly ICurrentUserService _currentUser = currentUser;

    public Task HandleAsync(CoursePublishedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            courseId = domainEvent.CourseId,
            ownerId = domainEvent.CourseOwnerId
        });

        var entry = AuditLogEntry.Create(
            eventType: nameof(CoursePublishedEvent),
            actorId: _currentUser.UserId,
            targetType: "Course",
            targetId: domainEvent.CourseId,
            payload: payload,
            occurredAt: domainEvent.OccurredAt
        );

        _db.AuditLogs.Add(entry);
        return Task.CompletedTask;
    }
}