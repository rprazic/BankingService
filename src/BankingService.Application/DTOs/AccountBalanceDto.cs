namespace BankingService.Application.DTOs;

public record AccountBalanceDto(
    Guid AccountId,
    MoneyDto Balance
);
