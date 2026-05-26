using BankingService.Domain.Enums;
using BankingService.Domain.Exceptions;
using BankingService.Domain.ValueObjects;
using FluentAssertions;

namespace BankingService.Unit.Tests.Domain;

public class MoneyArithmeticTests
{
    [Fact]
    public void AddMoney_SameCurrency_AddsAmounts()
    {
        var a = new Money(100m, Currency.EUR);
        var b = new Money(50m, Currency.EUR);

        var result = a + b;

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public void AddMoney_DifferentCurrency_ThrowsCurrencyMismatchException()
    {
        var eur = new Money(100m, Currency.EUR);
        var usd = new Money(50m, Currency.USD);

        var act = () => _ = eur + usd;

        act.Should().Throw<CurrencyMismatchException>();
    }

    [Fact]
    public void SubtractMoney_SameCurrency_SubtractsAmounts()
    {
        var a = new Money(100m, Currency.EUR);
        var b = new Money(40m, Currency.EUR);

        var result = a - b;

        result.Amount.Should().Be(60m);
        result.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public void SubtractMoney_DifferentCurrency_ThrowsCurrencyMismatchException()
    {
        var eur = new Money(100m, Currency.EUR);
        var usd = new Money(50m, Currency.USD);

        var act = () => _ = eur - usd;

        act.Should().Throw<CurrencyMismatchException>();
    }
}
