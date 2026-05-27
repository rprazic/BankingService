using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using FluentAssertions;

namespace BankingService.Unit.Tests.Domain;

public class MoneyScalarArithmeticTests
{
    [Fact]
    public void AddScalar_MoneyPlusDecimal_AddsAmount()
    {
        var money = new Money(100m, Currency.EUR);
        var result = money + 50m;

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public void AddScalar_DecimalPlusMoney_IsCommutative()
    {
        var money = new Money(100m, Currency.EUR);

        (50m + money).Should().Be(money + 50m);
    }

    [Fact]
    public void SubtractScalar_MoneyMinusDecimal_SubtractsAmount()
    {
        var money = new Money(100m, Currency.EUR);
        var result = money - 30m;

        result.Amount.Should().Be(70m);
        result.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public void SubtractScalar_DecimalMinusMoney_SubtractsFromDecimal()
    {
        var money = new Money(30m, Currency.EUR);
        var result = 100m - money;

        result.Amount.Should().Be(70m);
        result.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public void MultiplyScalar_RoundsUsingBankersRounding()
    {
        // 3.335 * 3 = 10.005 — midpoint rounds to nearest even (10.00, not 10.01)
        var money = new Money(3.335m, Currency.EUR);
        var result = money * 3m;

        result.Amount.Should().Be(10.00m);
    }

    [Fact]
    public void MultiplyScalar_DecimalTimesMoney_IsCommutative()
    {
        var money = new Money(100m, Currency.EUR);

        (3m * money).Should().Be(money * 3m);
    }

    [Fact]
    public void DivideScalar_DividesAmount()
    {
        var money = new Money(100m, Currency.EUR);
        var result = money / 4m;

        result.Amount.Should().Be(25m);
        result.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public void SubtractScalar_MoneyMinusDecimal_ProducesNegativeResult_Throws()
    {
        var money = new Money(50m, Currency.EUR);
        var act = () => { _ = money - 100m; };

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("amount");
    }

    [Fact]
    public void SubtractScalar_DecimalMinusMoney_ProducesNegativeResult_Throws()
    {
        var money = new Money(100m, Currency.EUR);
        var act = () => { _ = 30m - money; };

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("amount");
    }
}
