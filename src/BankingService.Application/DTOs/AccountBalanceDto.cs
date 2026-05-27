using System.ComponentModel;

namespace BankingService.Application.DTOs;

public record AccountBalanceDto(
    [property: Description("Unique account identifier.")] Guid AccountId,
    [property: Description("Current balance.")] MoneyDto Balance);
