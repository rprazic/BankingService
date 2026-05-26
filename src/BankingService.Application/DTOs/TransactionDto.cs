using BankingService.Domain.Enums;

namespace BankingService.Application.DTOs;

public record TransactionDto(
    Guid TransactionId,
    TransactionType Type,
    decimal Amount,
    Currency Currency,
    Guid? RelatedAccountId,
    string? Description,
    DateTime CreatedAt
);
