using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;

namespace BankingService.Domain.Entities;

public class Account
{
    public Guid AccountId { get; init; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Iban { get; init; } = string.Empty;
    public Currency Currency { get; init; }
    public Money Balance { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
}
