namespace RunRoutes.Core.Common.Queries;

public interface IQueryDispatcher
{
    Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}