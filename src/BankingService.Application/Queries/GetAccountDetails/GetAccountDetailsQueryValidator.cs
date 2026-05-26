using FluentValidation;

namespace BankingService.Application.Queries.GetAccountDetails;

public class GetAccountDetailsQueryValidator : AbstractValidator<GetAccountDetailsQuery>
{
    public GetAccountDetailsQueryValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("Account ID is required.");
    }
}
