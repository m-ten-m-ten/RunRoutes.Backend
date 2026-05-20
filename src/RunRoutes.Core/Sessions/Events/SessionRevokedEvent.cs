using RunRoutes.Core.Common.DomainEvents;

namespace RunRoutes.Core.Sessions.Events;

public record SessionRevokedEvent(Guid SessionId, Guid UserId, DateTime OccurredAt) : IDomainEvent;