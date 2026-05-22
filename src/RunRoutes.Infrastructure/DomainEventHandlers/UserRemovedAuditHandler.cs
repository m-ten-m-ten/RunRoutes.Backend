using System.Text.Json;
using RunRoutes.Core.Audit;
using RunRoutes.Core.Common;
using RunRoutes.Core.Common.DomainEvents;
using RunRoutes.Core.Users.Events;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.DomainEventHandlers;

public class UserRemovedAuditHandler(AppDbContext db, ICurrentUserService currentUser) : IDomainEventHandler<UserRemovedEvent>
{
    private readonly AppDbContext _db = db;
    private readonly ICurrentUserService _currentUser = currentUser;

    public Task HandleAsync(UserRemovedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            userId = domainEvent.UserId,
        });

        var entry = AuditLogEntry.Create(
            eventType: nameof(UserRemovedEvent),
            actorId: _currentUser.UserId,
            targetType: "User",
            targetId: domainEvent.UserId,
            payload: payload,
            occurredAt: domainEvent.OccurredAt
        );

        _db.AuditLogs.Add(entry);
        return Task.CompletedTask;
    }
}