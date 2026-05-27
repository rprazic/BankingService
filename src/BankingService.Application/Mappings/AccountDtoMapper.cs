using System.Linq.Expressions;
using BankingService.Application.DTOs;
using BankingService.Domain.Entities;

namespace BankingService.Application.Mappings;

public static class AccountDtoMapper
{
    public static AccountDto ToDto(this Account a) =>
        new(a.AccountId, a.FirstName, a.LastName, a.Iban, a.Balance.ToDto(), a.CreatedAt);

    public static Expression<Func<Account, AccountDto>> Projection() =>
        a => new AccountDto(
            a.AccountId,
            a.FirstName,
            a.LastName,
            a.Iban,
            new MoneyDto(a.Balance.Amount, a.Balance.Currency),
            a.CreatedAt);
}
