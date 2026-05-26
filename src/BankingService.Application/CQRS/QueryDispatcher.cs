using BankingService.Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BankingService.Application.CQRS;

public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _provider;

    public QueryDispatcher(IServiceProvider provider) => _provider = provider;

    public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken ct)
        where TQuery : IQuery<TResult>
    {
        var validators = _provider.GetServices<IValidator<TQuery>>().ToList();

        if (validators.Count > 0)
        {
            var context = new ValidationContext<TQuery>(query);
            var errors = validators
                .Select(v => v.Validate(context))
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .Select(f => f.ErrorMessage)
                .ToList();

            if (errors.Count > 0)
            {
                throw new BankingValidationException(errors);
            }
        }

        var handler = _provider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return await handler.HandleAsync(query, ct);
    }
}