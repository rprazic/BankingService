using System.ComponentModel;
using BankingService.Domain.Enums;

namespace BankingService.Api.Models;

public sealed record TransferRequest(
    [property: Description("Destination account identifier.")]
    Guid ToAccountId,
    [property: Description("Amount to transfer. Must be greater than 0.")]
    decimal Amount,
    [property: Description("Currency of the transfer. Must match both accounts' currency.")]
    Currency Currency,
    [property: Description("Optional free-text note, e.g. 'Rent payment'.")]
    string? Description = null);
