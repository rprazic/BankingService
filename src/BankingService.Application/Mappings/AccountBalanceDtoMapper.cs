using System.Linq.Expressions;
using BankingService.Application.DTOs;
using BankingService.Domain.Entities;

namespace BankingService.Application.Mappings;

public static class AccountBalanceDtoMapper
{
    public static AccountBalanceDto ToDto(this Account a) =>
        new(a.AccountId, a.Balance.ToDto());

    public static Expression<Func<Account, AccountBalanceDto>> Projection() =>
        a => new AccountBalanceDto(
            a.AccountId,
            new MoneyDto(a.Balance.Amount, a.Balance.Currency));
}