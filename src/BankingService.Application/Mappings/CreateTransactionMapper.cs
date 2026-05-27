using BankingService.Application.Commands.CreateTransaction;
using BankingService.Domain.Entities;

namespace BankingService.Application.Mappings;

public static class CreateTransactionMapper
{
    public static Transaction ToEntity(CreateTransactionCommand command) => new()
    {
        TransactionId = Guid.NewGuid(),
        AccountId = command.AccountId,
        Type = command.Type,
        Amount = command.Amount,
        Description = command.Description,
        RelatedAccountId = command.RelatedAccountId,
        CreatedAt = command.CreatedAt
    };
}