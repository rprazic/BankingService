using BankingService.Application.Commands.Deposit;
using BankingService.Application.Commands.Transfer;
using BankingService.Application.Commands.Withdraw;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using FluentAssertions;

namespace BankingService.Integration.Tests;

public class AccountConcurrencyTests : IntegrationTestBase
{
    [Fact]
    public async Task ConcurrentDeposits_TenSimultaneous_AllSucceedAndBalanceIsCorrect()
    {
        var accountId = await CreateAccountAsync("Test", "User");

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Sut.DepositAsync(
                new DepositCommand(accountId, new Money(100, Currency.EUR)), CancellationToken.None))
            .ToList();

        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        (await GetBalanceAsync(accountId)).Should().Be(1000m);
    }

    [Fact]
    public async Task ConcurrentWithdrawals_WhenInsufficientFunds_OnlyExpectedWithdrawalsSucceed()
    {
        var accountId = await CreateAccountAsync("Test", "User", initialDeposit: 500);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Sut.WithdrawAsync(
                new WithdrawCommand(accountId, new Money(100, Currency.EUR)), CancellationToken.None))
            .ToList();

        var results = await Task.WhenAll(tasks);

        results.Count(r => r.IsSuccess).Should().Be(5);
        results.Count(r => r.IsFailure).Should().Be(5);
        results.Where(r => r.IsFailure)
            .SelectMany(r => r.Errors)
            .Should().AllBe("Insufficient funds.");

        (await GetBalanceAsync(accountId)).Should().Be(0m);
    }

    [Fact]
    public async Task ConcurrentTransfers_BidirectionalBetweenTwoAccounts_NeitherDeadlocks()
    {
        var accountAId = await CreateAccountAsync("Alice", "Smith", initialDeposit: 1000);
        var accountBId = await CreateAccountAsync("Bob", "Jones", initialDeposit: 1000);

        var aToBTasks = Enumerable.Range(0, 10)
            .Select(_ => Sut.TransferAsync(
                new TransferCommand(accountAId, accountBId, new Money(50, Currency.EUR)), CancellationToken.None));

        var bToATasks = Enumerable.Range(0, 10)
            .Select(_ => Sut.TransferAsync(
                new TransferCommand(accountBId, accountAId, new Money(50, Currency.EUR)), CancellationToken.None));

        await Task.WhenAll(aToBTasks.Concat(bToATasks));

        var totalBalance = await GetBalanceAsync(accountAId) + await GetBalanceAsync(accountBId);
        totalBalance.Should().Be(2000m);
    }
}
