using FluentValidation;

namespace BankingService.Application.Queries.GetAccountBalance;

public class GetAccountBalanceQueryValidator : AbstractValidator<GetAccountBalanceQuery>
{
    public GetAccountBalanceQueryValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("Account ID is required.");
    }
}
