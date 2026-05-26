using BankingService.Domain.Enums;
using BankingService.Domain.Exceptions;
using BankingService.Domain.ValueObjects;
using FluentAssertions;

namespace BankingService.Unit.Tests.Domain;

public class MoneyComparisonTests
{
    // ── Scalar comparisons ───────────────────────────────────────────────────

    [Fact]
    public void GreaterThanDecimal_WhenAmountIsLarger_ReturnsTrue()
    {
        var money = new Money(100m, Currency.EUR);

        (money > 99m).Should().BeTrue();
        (money > 100m).Should().BeFalse();
    }

    [Fact]
    public void LessThanDecimal_WhenAmountIsSmaller_ReturnsTrue()
    {
        var money = new Money(50m, Currency.EUR);

        (money < 51m).Should().BeTrue();
        (money < 50m).Should().BeFalse();
    }

    [Fact]
    public void GreaterThanOrEqualDecimal_AtBoundary_ReturnsTrue()
    {
        var money = new Money(100m, Currency.EUR);

        (money >= 100m).Should().BeTrue();
        (money >= 101m).Should().BeFalse();
    }

    [Fact]
    public void LessThanOrEqualDecimal_AtBoundary_ReturnsTrue()
    {
        var money = new Money(100m, Currency.EUR);

        (money <= 100m).Should().BeTrue();
        (money <= 99m).Should().BeFalse();
    }

    // ── Money-to-Money comparisons ───────────────────────────────────────────

    [Fact]
    public void GreaterThanMoney_SameCurrency_ComparesAmounts()
    {
        var high = new Money(200m, Currency.EUR);
        var low = new Money(100m, Currency.EUR);

        (high > low).Should().BeTrue();
        (low > high).Should().BeFalse();
    }

    [Fact]
    public void GreaterThanMoney_DifferentCurrency_ThrowsCurrencyMismatchException()
    {
        var eur = new Money(200m, Currency.EUR);
        var usd = new Money(100m, Currency.USD);

        var act = () => _ = eur > usd;

        act.Should().Throw<CurrencyMismatchException>();
    }

    [Fact]
    public void LessThanMoney_SameCurrency_ComparesAmounts()
    {
        var low = new Money(100m, Currency.EUR);
        var high = new Money(200m, Currency.EUR);

        (low < high).Should().BeTrue();
        (high < low).Should().BeFalse();
    }

    [Fact]
    public void LessThanMoney_DifferentCurrency_ThrowsCurrencyMismatchException()
    {
        var eur = new Money(100m, Currency.EUR);
        var usd = new Money(200m, Currency.USD);

        var act = () => _ = eur < usd;

        act.Should().Throw<CurrencyMismatchException>();
    }

    [Fact]
    public void GreaterThanOrEqualMoney_AtEquality_ReturnsTrue()
    {
        var a = new Money(100m, Currency.EUR);
        var b = new Money(100m, Currency.EUR);

        (a >= b).Should().BeTrue();
    }

    [Fact]
    public void GreaterThanOrEqualMoney_DifferentCurrency_ThrowsCurrencyMismatchException()
    {
        var eur = new Money(100m, Currency.EUR);
        var usd = new Money(100m, Currency.USD);

        var act = () => _ = eur >= usd;

        act.Should().Throw<CurrencyMismatchException>();
    }

    [Fact]
    public void LessThanOrEqualMoney_AtEquality_ReturnsTrue()
    {
        var a = new Money(100m, Currency.EUR);
        var b = new Money(100m, Currency.EUR);

        (a <= b).Should().BeTrue();
    }

    [Fact]
    public void LessThanOrEqualMoney_DifferentCurrency_ThrowsCurrencyMismatchException()
    {
        var eur = new Money(100m, Currency.EUR);
        var usd = new Money(100m, Currency.USD);

        var act = () => _ = eur <= usd;

        act.Should().Throw<CurrencyMismatchException>();
    }
}
