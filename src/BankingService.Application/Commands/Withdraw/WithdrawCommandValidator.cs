using FluentValidation;

namespace BankingService.Application.Commands.Withdraw;

public class WithdrawCommandValidator : AbstractValidator<WithdrawCommand>
{
    public WithdrawCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.Amount.Amount)
            .GreaterThan(0).WithMessage("Withdrawal amount must be greater than zero.");

        RuleFor(x => x.Amount.Currency)
            .IsInEnum().WithMessage("Currency is not supported.");
    }
}
