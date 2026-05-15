namespace RunRoutes.Core.Common.DomainEvents;

/// <summary>
/// ドメインイベントを表すマーカーインターフェース。
/// 実装は record で「過去形」の名前にすること(例: CourseCreated, CommentPosted)。
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}