using BankingService.Application.DTOs;
using BankingService.Domain.ValueObjects;

namespace BankingService.Application.Mappings;

public static class MoneyDtoMapper
{
    public static MoneyDto ToDto(this Money money) =>
        new(money.Amount, money.Currency);
}