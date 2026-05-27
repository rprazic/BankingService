using FluentValidation;

namespace BankingService.Application.Commands.CreateTransaction;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).Must(m => m > 0).WithMessage("Amount must be greater than zero.");
    }
}
