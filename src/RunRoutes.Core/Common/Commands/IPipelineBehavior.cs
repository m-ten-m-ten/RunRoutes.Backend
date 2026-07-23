namespace RunRoutes.Core.Common.Commands;

public interface IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(
        TCommand command,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken);
}