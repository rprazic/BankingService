using System.ComponentModel;
using BankingService.Domain.Enums;

namespace BankingService.Api.Models;

public sealed record DepositRequest(
    [property: Description("Amount to deposit. Must be greater than 0.")]
    decimal Amount,
    [property: Description("Currency of the deposit. Must match the account's currency.")]
    Currency Currency);