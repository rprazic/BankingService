using BankingService.Application.Commands.CreateAccount;
using BankingService.Domain.Entities;
using BankingService.Domain.ValueObjects;

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
        Balance = new Money(command.InitialDeposit),
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    };
}