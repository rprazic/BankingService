using FluentValidation;

namespace BankingService.Application.Commands.CreateAccount;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.InitialDeposit.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Initial deposit must be greater than or equal to zero.");

        RuleFor(x => x.InitialDeposit.Currency)
            .IsInEnum().WithMessage("Currency is not supported.");
    }
}
