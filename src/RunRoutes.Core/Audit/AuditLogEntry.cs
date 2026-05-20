namespace RunRoutes.Core.Audit;

/// <summary>
/// 監査ログのエントリ。集約ではない単純なエンティティ。
/// ドメインイベントハンドラから生成される。
/// </summary>
public class AuditLogEntry
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = default!;
    public Guid? ActorId { get; private set; }
    public string TargetType { get; private set; } = default!;
    public Guid TargetId { get; private set; }
    public string Payload { get; private set; } = default!;  // jsonb 文字列として保持
    public DateTime OccurredAt { get; private set; }

    private AuditLogEntry() { }  // EF Core 用

    public static AuditLogEntry Create(
        string eventType,
        Guid? actorId,
        string targetType,
        Guid targetId,
        string payload,
        DateTime occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            ActorId = actorId,
            TargetType = targetType,
            TargetId = targetId,
            Payload = payload,
            OccurredAt = occurredAt
        };
    }
}