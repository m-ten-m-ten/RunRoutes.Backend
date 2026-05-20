using RunRoutes.Core.Common.DomainEvents;

namespace RunRoutes.Core.Sessions.Events;

public record SessionStartedEvent(Guid SessionId, Guid UserId, DateTime OccurredAt) : IDomainEvent;