using BankingService.Domain.Enums;

namespace BankingService.Application.DTOs;

public record AccountBalanceDto(
    Guid AccountId,
    decimal Balance,
    Currency Currency
);
