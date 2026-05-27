using System.Text.Json;
using RunRoutes.Core.Audit;
using RunRoutes.Core.Common;
using RunRoutes.Core.Common.DomainEvents;
using RunRoutes.Core.Sessions.Events;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.DomainEventHandlers;

public class SessionStartedAuditHandler(AppDbContext db, ICurrentUserService currentUser)
    : IDomainEventHandler<SessionStartedEvent>
{
    private readonly AppDbContext _db = db;
    private readonly ICurrentUserService _currentUser = currentUser;

    public Task HandleAsync(SessionStartedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var entry = AuditLogEntry.Create(
            eventType: nameof(SessionStartedEvent),
            actorId: _currentUser.UserId,
            targetType: "Session",
            targetId: domainEvent.SessionId,
            payload: JsonSerializer.Serialize(new { userId = domainEvent.UserId }),
            occurredAt: domainEvent.OccurredAt
        );

        _db.AuditLogs.Add(entry);
        return Task.CompletedTask;
    }
}