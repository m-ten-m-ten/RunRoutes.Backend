using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RunRoutes.Core.Common.DomainEvents;
using RunRoutes.Infrastructure.DomainEvents;

namespace RunRoutes.Core.Tests.DomainEvents;

// namespace レベルに公開することで Moq の DynamicProxy からアクセス可能にする
public sealed record TestEvent(DateTime OccurredAt) : IDomainEvent;

public class DomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_登録されたハンドラが全て呼ばれる()
    {
        var handler1 = new Mock<IDomainEventHandler<TestEvent>>();
        var handler2 = new Mock<IDomainEventHandler<TestEvent>>();

        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<TestEvent>>(handler1.Object);
        services.AddSingleton<IDomainEventHandler<TestEvent>>(handler2.Object);
        var provider = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(provider, NullLogger<DomainEventDispatcher>.Instance);

        var evt = new TestEvent(DateTime.UtcNow);
        await dispatcher.DispatchAsync([evt]);

        handler1.Verify(h => h.HandleAsync(evt, It.IsAny<CancellationToken>()), Times.Once);
        handler2.Verify(h => h.HandleAsync(evt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ハンドラが例外を投げても他のハンドラは実行される()
    {
        var handler1 = new Mock<IDomainEventHandler<TestEvent>>();
        handler1.Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("intentional"));
        var handler2 = new Mock<IDomainEventHandler<TestEvent>>();

        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<TestEvent>>(sp => handler1.Object);
        services.AddSingleton<IDomainEventHandler<TestEvent>>(sp => handler2.Object);
        var provider = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(provider, NullLogger<DomainEventDispatcher>.Instance);
        var evt = new TestEvent(DateTime.UtcNow);

        // 例外が外に漏れないことを確認
        await dispatcher.DispatchAsync([evt]);

        handler2.Verify(h => h.HandleAsync(evt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ハンドラが登録されていないイベントは無視される()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider, NullLogger<DomainEventDispatcher>.Instance);

        // 例外を投げずに正常終了することを確認
        await dispatcher.DispatchAsync([new TestEvent(DateTime.UtcNow)]);
    }
}
