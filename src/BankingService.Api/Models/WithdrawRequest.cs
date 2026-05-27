using System.ComponentModel;
using BankingService.Domain.Enums;

namespace BankingService.Api.Models;

public sealed record WithdrawRequest(
    [property: Description("Amount to withdraw. Must be greater than 0.")]
    decimal Amount,
    [property: Description("Currency of the withdrawal. Must match the account's currency.")]
    Currency Currency);
