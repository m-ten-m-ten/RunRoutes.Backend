using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunRoutes.Core.Common.DomainEvents;

namespace RunRoutes.Infrastructure.DomainEvents;

public class DomainEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger = logger;

    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null) continue;

                try
                {
                    var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                    await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Domain event handler {Handler} failed for event {Event}",
                        handler.GetType().Name, domainEvent.GetType().Name);
                    // 続行
                }
            }
        }
    }
}