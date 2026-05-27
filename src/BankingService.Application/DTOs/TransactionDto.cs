using System.ComponentModel;
using BankingService.Domain.Enums;

namespace BankingService.Application.DTOs;

public record TransactionDto(
    [property: Description("Unique transaction identifier.")] Guid TransactionId,
    [property: Description("Credit = money in, Debit = money out.")] TransactionType Type,
    [property: Description("Transaction amount and currency.")] MoneyDto Amount,
    [property: Description("The other account involved in the transaction (transfers only).")] Guid? RelatedAccountId,
    [property: Description("Optional free-text note attached to the transaction.")] string? Description,
    [property: Description("UTC timestamp when the transaction was recorded.")] DateTime CreatedAt);
