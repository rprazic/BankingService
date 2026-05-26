namespace BankingService.Application.DTOs;

public record AccountDto(
    Guid AccountId,
    string FirstName,
    string LastName,
    string Iban,
    MoneyDto Balance,
    DateTime CreatedAt
);
