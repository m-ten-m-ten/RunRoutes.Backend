using RunRoutes.Core.Common.DomainEvents;

namespace RunRoutes.Core.Common;

/// <summary>
/// 集約ルートの基底クラス。
/// 集約内で発生したドメインイベントを溜める器を提供する。
/// イベントの実際のディスパッチは SaveChangesAsync オーバーライドにて行う。
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// 未ディスパッチのドメインイベント一覧(読み取り専用)。
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// ドメインイベントを追加する。集約内のメソッドからのみ呼ぶことを想定。
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 溜まっているドメインイベントをクリアする。
    /// ディスパッチ後に呼ばれる想定。
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}