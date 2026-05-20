using RunRoutes.Core.Common.DomainEvents;

namespace RunRoutes.Core.Courses.Events;

public sealed record CoursePublishedEvent(
    Guid CourseId,
    Guid CourseOwnerId,
    DateTime OccurredAt
) : IDomainEvent;