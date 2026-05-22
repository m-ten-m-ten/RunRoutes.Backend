using RunRoutes.Core.Common.DomainEvents;

namespace RunRoutes.Core.Users.Events;

public sealed record UserRemovedEvent(
    Guid UserId,
    DateTime OccurredAt
) : IDomainEvent;