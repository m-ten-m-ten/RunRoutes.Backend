using RunRoutes.Core.Common.DomainEvents;

namespace RunRoutes.Core.Courses.Events;

public sealed record CommentAddedEvent(
    Guid CommentId,
    Guid CourseId,
    Guid CommentAuthorId,
    DateTime OccurredAt
) : IDomainEvent;