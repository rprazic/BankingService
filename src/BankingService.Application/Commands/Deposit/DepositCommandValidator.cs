using FluentValidation;

namespace BankingService.Application.Commands.Deposit;

public class DepositCommandValidator : AbstractValidator<DepositCommand>
{
    public DepositCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.Amount.Amount)
            .GreaterThan(0).WithMessage("Deposit amount must be greater than zero.");

        RuleFor(x => x.Amount.Currency)
            .IsInEnum().WithMessage("Currency is not supported.");
    }
}
