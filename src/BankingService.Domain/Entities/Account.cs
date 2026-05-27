using BankingService.Domain.Enums;
using BankingService.Domain.Exceptions;
using BankingService.Domain.ValueObjects;

namespace BankingService.Domain.Entities;

public class Account
{
    private Money _balance = null!;
    private DateTime _updatedAt;

    public Guid AccountId { get; init; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Iban { get; init; } = string.Empty;
    public Currency Currency { get; init; }

    public Money Balance
    {
        get => _balance;
        init => _balance = value;
    }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt
    {
        get => _updatedAt;
        init => _updatedAt = value;
    }

    public void Deposit(Money amount, DateTime now)
    {
        _balance += amount;
        _updatedAt = now;
    }

    public void Withdraw(Money amount, DateTime now)
    {
        if (amount > _balance)
        {
            throw new InsufficientFundsException();
        }

        _balance -= amount;
        _updatedAt = now;
    }
}