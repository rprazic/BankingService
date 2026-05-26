using BankingService.Application.Common;
using BankingService.Application.CQRS;
using FluentValidation;

namespace BankingService.Application.Behaviours;

public class ValidationCommandHandlerDecorator<TCommand, TResult>
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _inner;
    private readonly IEnumerable<IValidator<TCommand>> _validators;

    public ValidationCommandHandlerDecorator(ICommandHandler<TCommand, TResult> inner,
        IEnumerable<IValidator<TCommand>> validators)
    {
        _inner = inner;
        _validators = validators;
    }

    public async Task<TResult> HandleAsync(TCommand command, CancellationToken ct, bool saveChanges = true)
    {
        var validators = _validators.ToList();

        if (validators.Count <= 0)
        {
            return await _inner.HandleAsync(command, ct, saveChanges);
        }

        var context = new ValidationContext<TCommand>(command);
        var errors = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => f.ErrorMessage)
            .ToList();

        return errors.Count > 0
            ? throw new BankingValidationException(errors)
            : await _inner.HandleAsync(command, ct, saveChanges);
    }
}