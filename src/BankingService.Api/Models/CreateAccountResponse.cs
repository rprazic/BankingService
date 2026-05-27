using System.ComponentModel;

namespace BankingService.Api.Models;

public sealed record CreateAccountResponse(
    [property: Description("Newly created account's unique identifier.")]
    Guid AccountId);
