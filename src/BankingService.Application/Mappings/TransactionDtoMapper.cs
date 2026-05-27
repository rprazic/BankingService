using System.Linq.Expressions;
using BankingService.Application.DTOs;
using BankingService.Domain.Entities;

namespace BankingService.Application.Mappings;

public static class TransactionDtoMapper
{
    public static TransactionDto ToDto(this Transaction t) =>
        new(t.TransactionId, t.Type, t.Amount.ToDto(), t.RelatedAccountId, t.Description, t.CreatedAt);

    public static Expression<Func<Transaction, TransactionDto>> Projection() =>
        t => new TransactionDto(
            t.TransactionId,
            t.Type,
            new MoneyDto(t.Amount.Amount, t.Amount.Currency),
            t.RelatedAccountId,
            t.Description,
            t.CreatedAt);
}
