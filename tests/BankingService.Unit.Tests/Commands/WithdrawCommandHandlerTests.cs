using BankingService.Application.Commands.Withdraw;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TransactionType = BankingService.Domain.Enums.TransactionType;

namespace BankingService.Unit.Tests.Commands;

public class WithdrawCommandHandlerTests : BankingDbContextTestBase
{
    private readonly WithdrawCommandHandler _sut;

    public WithdrawCommandHandlerTests()
    {
        _sut = new WithdrawCommandHandler(Context, CreateDispatcher(), NullLogger<WithdrawCommandHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSuccessResult()
    {
        var account = CreateAccount(balance: 500m);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new WithdrawCommand(account.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsNewBalance()
    {
        var account = CreateAccount(balance: 500m);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new WithdrawCommand(account.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        result.Value!.Amount.Should().Be(300m);
        result.Value.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_DecreasesAccountBalance()
    {
        var account = CreateAccount(balance: 500m);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        await _sut.HandleAsync(new WithdrawCommand(account.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        var updated = await Context.Accounts.FindAsync(account.AccountId);
        updated!.Balance.Amount.Should().Be(300m);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesDebitTransaction()
    {
        var account = CreateAccount(balance: 500m);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        await _sut.HandleAsync(new WithdrawCommand(account.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        var transaction = await Context.Transactions.FirstOrDefaultAsync();
        transaction.Should().NotBeNull();
        transaction!.AccountId.Should().Be(account.AccountId);
        transaction.Type.Should().Be(TransactionType.Debit);
        transaction.Amount.Amount.Should().Be(200m);
        transaction.Description.Should().Be("Withdrawal");
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ReturnsFailure()
    {
        var result = await _sut.HandleAsync(new WithdrawCommand(Guid.NewGuid(), new Money(200m, Currency.EUR)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Account not found.");
    }

    [Fact]
    public async Task HandleAsync_InactiveAccount_ReturnsFailure()
    {
        var account = CreateAccount(balance: 500m, isActive: false);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new WithdrawCommand(account.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Account is not active.");
    }

    [Fact]
    public async Task HandleAsync_CurrencyMismatch_ReturnsFailure()
    {
        var account = CreateAccount(balance: 500m); // EUR account
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new WithdrawCommand(account.AccountId, new Money(200m, Currency.USD)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Withdrawal currency does not match account currency.");
    }

    [Fact]
    public async Task HandleAsync_InsufficientFunds_ReturnsFailure()
    {
        var account = CreateAccount(balance: 100m);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new WithdrawCommand(account.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Insufficient funds.");
    }
}