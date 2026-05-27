using System.ComponentModel;
using BankingService.Domain.Enums;

namespace BankingService.Api.Models;

public sealed record CreateAccountRequest(
    [property: Description("Account holder's first name. Max 100 characters.")]
    string FirstName,
    [property: Description("Account holder's last name. Max 100 characters.")]
    string LastName,
    [property: Description("Initial deposit amount. Must be 0 or greater.")]
    decimal InitialDeposit,
    [property: Description("Account currency as ISO 4217 numeric code (978 = EUR, 840 = USD, 941 = RSD).")]
    Currency Currency);
