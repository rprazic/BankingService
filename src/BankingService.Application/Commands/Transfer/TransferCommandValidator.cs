using FluentValidation;

namespace BankingService.Application.Commands.Transfer;

public class TransferCommandValidator : AbstractValidator<TransferCommand>
{
    public TransferCommandValidator()
    {
        RuleFor(x => x.FromAccountId)
            .NotEmpty().WithMessage("Source account ID is required.");

        RuleFor(x => x.ToAccountId)
            .NotEmpty().WithMessage("Destination account ID is required.");

        RuleFor(x => x.FromAccountId)
            .NotEqual(x => x.ToAccountId).WithMessage("Source and destination accounts must differ.");

        RuleFor(x => x.Amount.Amount)
            .GreaterThan(0).WithMessage("Transfer amount must be greater than zero.");

        RuleFor(x => x.Amount.Currency)
            .IsInEnum().WithMessage("Currency is not supported.");
    }
}
