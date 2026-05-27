using System.ComponentModel;
using BankingService.Domain.Enums;

namespace BankingService.Application.DTOs;

public record MoneyDto(
    [property: Description("Monetary amount.")] decimal Amount,
    [property: Description("Currency as ISO 4217 numeric code (978 = EUR, 840 = USD, 941 = RSD).")] Currency Currency);
