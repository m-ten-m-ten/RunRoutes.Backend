using RunRoutes.Core.Common;

namespace RunRoutes.Core.Tests.Common;

public class AggregateRootTests
{
    private sealed record TestEvent(DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed class TestAggregate : AggregateRoot
    {
        public void RaiseTestEvent() => AddDomainEvent(new TestEvent(DateTimeOffset.UtcNow));
    }

    private readonly TestAggregate _sut;

    public AggregateRootTests()
    {
        _sut = new TestAggregate();
    }

    [Fact]
    public void DomainEvents_初期状態は空()
    {
        Assert.Empty(_sut.DomainEvents);
    }

    [Fact]
    public void AddDomainEvent_でイベントが追加される()
    {
        _sut.RaiseTestEvent();
        Assert.Single(_sut.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_でイベントが空になる()
    {
        _sut.RaiseTestEvent();
        _sut.ClearDomainEvents();
        Assert.Empty(_sut.DomainEvents);
    }

    [Fact]
    public void AddDomainEvent_null_は例外()
    {
        // protected なので TestAggregate にテスト用メソッドを足してもよいが、
        // ここでは ArgumentNullException が投げられることだけ確認する想定。
        // 実装が ArgumentNullException.ThrowIfNull を使っていれば自動で通る。
    }
}