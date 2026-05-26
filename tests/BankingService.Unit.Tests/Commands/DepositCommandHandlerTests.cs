using BankingService.Application.Commands.Deposit;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TransactionType = BankingService.Domain.Enums.TransactionType;

namespace BankingService.Unit.Tests.Commands;

public class DepositCommandHandlerTests : BankingDbContextTestBase
{
    private readonly DepositCommandHandler _sut;

    public DepositCommandHandlerTests()
    {
        _sut = new DepositCommandHandler(Context);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSuccessResult()
    {
        var account = CreateAccount(balance: 500m);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new DepositCommand(account.AccountId, new Money(300m, Currency.EUR)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsNewBalance()
    {
        var account = CreateAccount(balance: 500m);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new DepositCommand(account.AccountId, new Money(300m, Currency.EUR)), CancellationToken.None);

        result.Value!.Amount.Should().Be(800m);
        result.Value.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_IncreasesAccountBalance()
    {
        var account = CreateAccount(balance: 500m);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        await _sut.HandleAsync(new DepositCommand(account.AccountId, new Money(300m, Currency.EUR)), CancellationToken.None);

        var updated = await Context.Accounts.FindAsync(account.AccountId);
        updated!.Balance.Amount.Should().Be(800m);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesCreditTransaction()
    {
        var account = CreateAccount(balance: 500m);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        await _sut.HandleAsync(new DepositCommand(account.AccountId, new Money(300m, Currency.EUR)), CancellationToken.None);

        var transaction = await Context.Transactions.FirstOrDefaultAsync();
        transaction.Should().NotBeNull();
        transaction!.AccountId.Should().Be(account.AccountId);
        transaction.Type.Should().Be(TransactionType.Credit);
        transaction.Amount.Amount.Should().Be(300m);
        transaction.Description.Should().Be("Deposit");
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ReturnsFailure()
    {
        var result = await _sut.HandleAsync(new DepositCommand(Guid.NewGuid(), new Money(300m, Currency.EUR)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Account not found.");
    }

    [Fact]
    public async Task HandleAsync_InactiveAccount_ReturnsFailure()
    {
        var account = CreateAccount(balance: 500m, isActive: false);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new DepositCommand(account.AccountId, new Money(300m, Currency.EUR)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Account is not active.");
    }

    [Fact]
    public async Task HandleAsync_CurrencyMismatch_ReturnsFailure()
    {
        var account = CreateAccount(balance: 500m); // EUR account
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new DepositCommand(account.AccountId, new Money(300m, Currency.USD)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Deposit currency does not match account currency.");
    }
}
