using Microsoft.Extensions.Logging;
using RunRoutes.Core.Common.Commands;

namespace RunRoutes.Infrastructure.Behaviors;

public class LoggingBehavior<TCommand, TResponse>(
    ILogger<LoggingBehavior<TCommand, TResponse>> logger)
    : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TCommand command,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TCommand).Name;
        logger.LogInformation("Command 開始: {CommandName}", name);

        try
        {
            var response = await next(cancellationToken);
            logger.LogInformation("Command 完了: {CommandName}", name);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Command 失敗: {CommandName}", name);
            throw;
        }
    }
}