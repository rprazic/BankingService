using BankingService.Domain.Enums;

namespace BankingService.Application.DTOs;

public record MoneyDto(decimal Amount, Currency Currency);
