using System.Text.Json;
using RunRoutes.Core.Audit;
using RunRoutes.Core.Common;
using RunRoutes.Core.Common.DomainEvents;
using RunRoutes.Core.Courses.Events;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.DomainEventHandlers;

public class CommentAddedAuditHandler(AppDbContext db, ICurrentUserService currentUser) : IDomainEventHandler<CommentAddedEvent>
{
    private readonly AppDbContext _db = db;
    private readonly ICurrentUserService _currentUser = currentUser;

    public Task HandleAsync(CommentAddedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            commentId = domainEvent.CommentId,
            courseId = domainEvent.CourseId,
            authorId = domainEvent.CommentAuthorId
        });

        var entry = AuditLogEntry.Create(
            eventType: nameof(CommentAddedEvent),
            actorId: _currentUser.UserId,
            targetType: "Comment",
            targetId: domainEvent.CommentId,
            payload: payload,
            occurredAt: domainEvent.OccurredAt
        );

        _db.AuditLogs.Add(entry);
        return Task.CompletedTask;
    }
}