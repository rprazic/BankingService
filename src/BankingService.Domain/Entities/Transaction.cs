using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;

namespace BankingService.Domain.Entities;

public class Transaction
{
    public Guid TransactionId { get; init; }
    public Guid AccountId { get; init; }
    public TransactionType Type { get; init; }
    public Money Amount { get; init; } = null!;
    public Guid? RelatedAccountId { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
}
