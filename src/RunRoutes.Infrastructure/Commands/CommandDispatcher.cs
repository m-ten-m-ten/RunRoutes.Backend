using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RunRoutes.Core.Common.Commands;

namespace RunRoutes.Infrastructure.Commands;

public class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private static readonly MethodInfo SendInternalMethod =
        typeof(CommandDispatcher).GetMethod(
            nameof(SendInternalAsync),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    public Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 実行時にしか分からない TCommand の具体型を以て、ここで SendInternalAsync() の型引数を埋める
        var closed = SendInternalMethod.MakeGenericMethod(command.GetType(), typeof(TResponse));

        return (Task<TResponse>)closed.Invoke(this, [command, cancellationToken])!;
    }

    // ここから先は TCommand が普通の型引数として使える世界
    private async Task<TResponse> SendInternalAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();

        var behaviors = _serviceProvider
            .GetServices<IPipelineBehavior<TCommand, TResponse>>()
            .Reverse()
            .ToList();

        // 最奥 = Handler 本体
        Func<CancellationToken, Task<TResponse>> pipeline =
            ct => handler.HandleAsync(command, ct);

        // 内側から外側へ、順に包んでいく
        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            var current = behavior;
            pipeline = ct => current.HandleAsync(command, next, ct);
        }

        return await pipeline(cancellationToken);
    }
}