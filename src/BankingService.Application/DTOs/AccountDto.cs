using System.ComponentModel;

namespace BankingService.Application.DTOs;

public record AccountDto(
    [property: Description("Unique account identifier.")] Guid AccountId,
    [property: Description("Account holder's first name.")] string FirstName,
    [property: Description("Account holder's last name.")] string LastName,
    [property: Description("IBAN assigned to this account.")] string Iban,
    [property: Description("Current balance.")] MoneyDto Balance,
    [property: Description("UTC timestamp when the account was created.")] DateTime CreatedAt);
