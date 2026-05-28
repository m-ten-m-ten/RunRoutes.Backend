namespace RunRoutes.Core.Common.Commands;

public interface ICommandDispatcher
{
    Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
}