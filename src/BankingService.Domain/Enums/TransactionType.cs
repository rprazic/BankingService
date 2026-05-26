namespace BankingService.Domain.Enums;

/// <summary>
/// Represents the direction of money flow on an account.
/// Credit = money arriving (deposit, transfer in).
/// Debit  = money leaving (withdrawal, transfer out).
/// </summary>
public enum TransactionType
{
    Credit,
    Debit
}