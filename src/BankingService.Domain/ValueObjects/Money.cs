using BankingService.Domain.Enums;
using BankingService.Domain.Exceptions;

namespace BankingService.Domain.ValueObjects;

public class Money : BaseValueObject
{
    public Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public Money(Money money)
    {
        Amount = money.Amount;
        Currency = money.Currency;
    }

    // Private — for EF Core materialization only
    private Money()
    {
        Amount = 0m;
        Currency = default;
    }

    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }

    public Money ToAmount(decimal amount) => new(amount, Currency);

    // Scalar arithmetic
    public static Money operator +(Money m, decimal d) => new(m.Amount + d, m.Currency);
    public static Money operator +(decimal d, Money m) => m + d;

    public static Money operator -(Money m, decimal d) => new(m.Amount - d, m.Currency);
    public static Money operator -(decimal d, Money m) => new(d - m.Amount, m.Currency);

    public static Money operator *(Money m, decimal d) => new(RoundEven(m.Amount * d), m.Currency);
    public static Money operator *(decimal d, Money m) => m * d;

    /// <summary>Money division by a scalar.</summary>
    public static Money operator /(Money m, decimal d) => new(m.Amount / d, m.Currency);

    // Scalar comparisons
    public static bool operator >(Money m, decimal d) => m.Amount > d;
    public static bool operator <(Money m, decimal d) => m.Amount < d;
    public static bool operator >=(Money m, decimal d) => m.Amount >= d;
    public static bool operator <=(Money m, decimal d) => m.Amount <= d;

    // Money operations
    public static Money operator +(Money m1, Money m2)
    {
        RequireSameCurrency(m1, m2);
        return m1 + m2.Amount;
    }

    public static Money operator -(Money m1, Money m2)
    {
        RequireSameCurrency(m1, m2);
        return m1 - m2.Amount;
    }

    // Money comparisons (same currency)
    public static bool operator >(Money m1, Money m2)
    {
        RequireSameCurrency(m1, m2);
        return m1.Amount > m2.Amount;
    }

    public static bool operator <(Money m1, Money m2)
    {
        RequireSameCurrency(m1, m2);
        return m1.Amount < m2.Amount;
    }

    public static bool operator >=(Money m1, Money m2)
    {
        RequireSameCurrency(m1, m2);
        return m1.Amount >= m2.Amount;
    }

    public static bool operator <=(Money m1, Money m2)
    {
        RequireSameCurrency(m1, m2);
        return m1.Amount <= m2.Amount;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    /// <summary>
    /// Banker's rounding — rounds to nearest even on midpoint.
    /// Standard in financial calculations to minimize cumulative rounding bias.
    /// </summary>
    private static decimal RoundEven(decimal amount)
        => Math.Round(amount, 2, MidpointRounding.ToEven);

    private static void RequireSameCurrency(Money m1, Money m2)
    {
        if (m1.Currency == m2.Currency)
        {
            return;
        }

        throw new CurrencyMismatchException(m1.Currency, m2.Currency);
    }
}