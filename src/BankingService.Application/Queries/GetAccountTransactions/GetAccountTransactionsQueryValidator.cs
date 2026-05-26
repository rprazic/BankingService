using FluentValidation;

namespace BankingService.Application.Queries.GetAccountTransactions;

public class GetAccountTransactionsQueryValidator : AbstractValidator<GetAccountTransactionsQuery>
{
    public GetAccountTransactionsQueryValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("Account ID is required.");
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be greater than zero.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
