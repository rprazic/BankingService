using BankingService.Domain.Enums;

namespace BankingService.Application.DTOs;

public record TransactionDto(
    Guid TransactionId,
    TransactionType Type,
    MoneyDto Amount,
    Guid? RelatedAccountId,
    string? Description,
    DateTime CreatedAt
);
