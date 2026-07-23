using FluentValidation;
using RunRoutes.Core.Common.Commands;
using ValidationException = RunRoutes.Core.Common.Exceptions.ValidationException;

namespace RunRoutes.Infrastructure.Behaviors;

public class ValidationBehavior<TCommand, TResponse>(
    IEnumerable<IValidator<TCommand>> validators)
    : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TCommand command,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TCommand>(command);

        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            var errors = failures
                .GroupBy(f => f.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(f => f.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }

        return await next(cancellationToken);
    }
}