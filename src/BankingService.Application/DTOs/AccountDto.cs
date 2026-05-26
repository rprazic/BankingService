using BankingService.Domain.Enums;

namespace BankingService.Application.DTOs;

public record AccountDto(
    Guid AccountId,
    string FirstName,
    string LastName,
    string Iban,
    Currency Currency,
    decimal Balance,
    DateTime CreatedAt
);
