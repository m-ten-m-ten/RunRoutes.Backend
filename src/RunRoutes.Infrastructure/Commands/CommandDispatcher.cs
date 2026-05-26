using Microsoft.Extensions.DependencyInjection;
using RunRoutes.Core.Common.Commands;

namespace RunRoutes.Infrastructure.Commands;

public class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<,>)
            .MakeGenericType(command.GetType(), typeof(TResponse));

        var handler = _serviceProvider.GetRequiredService(handlerType);

        var method = handlerType.GetMethod(
            nameof(ICommandHandler<ICommand<TResponse>, TResponse>.HandleAsync))!;

        var task = (Task<TResponse>)method.Invoke(handler, [command, cancellationToken])!;
        return await task;
    }
}