using System.Text.Json;
using RunRoutes.Core.Audit;
using RunRoutes.Core.Common;
using RunRoutes.Core.Common.DomainEvents;
using RunRoutes.Core.Sessions.Events;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.DomainEventHandlers;

public class SessionRevokedAuditHandler(AppDbContext db, ICurrentUserService currentUser)
    : IDomainEventHandler<SessionRevokedEvent>
{
    private readonly AppDbContext _db = db;
    private readonly ICurrentUserService _currentUser = currentUser;

    public Task HandleAsync(SessionRevokedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var entry = AuditLogEntry.Create(
            eventType: nameof(SessionRevokedEvent),
            actorId: _currentUser.UserId,
            targetType: "Session",
            targetId: domainEvent.SessionId,
            payload: JsonSerializer.Serialize(new { domainEvent.UserId }),
            occurredAt: domainEvent.OccurredAt
        );

        _db.AuditLogs.Add(entry);
        return Task.CompletedTask;
    }
}