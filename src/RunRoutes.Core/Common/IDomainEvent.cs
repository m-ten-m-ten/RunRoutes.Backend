namespace RunRoutes.Core.Common;

/// <summary>
/// ドメインイベントを表すマーカーインターフェース。
/// 実装は record で「過去形」の名前にすること(例: CourseCreated, CommentPosted)。
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}