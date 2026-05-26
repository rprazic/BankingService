using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using FluentAssertions;

namespace BankingService.Unit.Tests.Domain;

public class MoneyConstructionTests
{
    [Fact]
    public void Constructor_SetsAmountAndCurrency()
    {
        var money = new Money(100m, Currency.EUR);

        money.Amount.Should().Be(100m);
        money.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public void CopyConstructor_ProducesEqualInstance()
    {
        var original = new Money(250m, Currency.USD);
        var copy = new Money(original);

        copy.Should().Be(original);
        copy.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Equals_SameAmountAndCurrency_ReturnsTrue()
    {
        var a = new Money(100m, Currency.EUR);
        var b = new Money(100m, Currency.EUR);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentAmount_ReturnsFalse()
    {
        var a = new Money(100m, Currency.EUR);
        var b = new Money(200m, Currency.EUR);

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentCurrency_ReturnsFalse()
    {
        var a = new Money(100m, Currency.EUR);
        var b = new Money(100m, Currency.USD);

        a.Should().NotBe(b);
    }

    [Fact]
    public void GetHashCode_EqualInstances_ReturnSameHash()
    {
        var a = new Money(100m, Currency.EUR);
        var b = new Money(100m, Currency.EUR);

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ToAmount_ReplacesAmount_KeepsCurrency()
    {
        var original = new Money(100m, Currency.EUR);
        var result = original.ToAmount(500m);

        result.Amount.Should().Be(500m);
        result.Currency.Should().Be(Currency.EUR);
    }
}
