using BankingService.Application.Commands.CreateAccount;
using BankingService.Domain.Entities;

namespace BankingService.Application.Mappings;

public static class CreateAccountMapper
{
    public static Account ToEntity(CreateAccountCommand command, string iban, DateTime now) => new()
    {
        AccountId = Guid.NewGuid(),
        FirstName = command.FirstName,
        LastName = command.LastName,
        Iban = iban,
        Currency = command.InitialDeposit.Currency,
        Balance = command.InitialDeposit,
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    };
}