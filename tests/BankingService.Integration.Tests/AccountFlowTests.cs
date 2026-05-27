using BankingService.Application.Commands.Deposit;
using BankingService.Application.Commands.Transfer;
using BankingService.Application.Commands.Withdraw;
using BankingService.Application.Queries.GetAccountDetails;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using FluentAssertions;

namespace BankingService.Integration.Tests;

public class AccountFlowTests : IntegrationTestBase
{
    [Fact]
    public async Task FullFlow_CreateDepositWithdraw_BalanceAndTransactionsCorrect()
    {
        var accountId = await CreateAccountAsync("Alice", "Smith");

        (await Sut.DepositAsync(
            new DepositCommand(accountId, new Money(500, Currency.EUR)), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        (await Sut.WithdrawAsync(
            new WithdrawCommand(accountId, new Money(200, Currency.EUR)), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        (await GetBalanceAsync(accountId)).Should().Be(300m);

        var transactions = await GetTransactionsAsync(accountId);
        transactions.TotalCount.Should().Be(2);
        transactions.Items.Should().ContainSingle(t => t.Type == TransactionType.Credit);
        transactions.Items.Should().ContainSingle(t => t.Type == TransactionType.Debit);
    }

    [Fact]
    public async Task FullFlow_Transfer_BothBalancesAndTransactionLogsUpdated()
    {
        var accountAId = await CreateAccountAsync("Alice", "Smith", initialDeposit: 1000);
        var accountBId = await CreateAccountAsync("Bob", "Jones");

        (await Sut.TransferAsync(
            new TransferCommand(accountAId, accountBId, new Money(400, Currency.EUR)), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        (await GetBalanceAsync(accountAId)).Should().Be(600m);
        (await GetBalanceAsync(accountBId)).Should().Be(400m);

        var txA = await GetTransactionsAsync(accountAId);
        txA.Items.Should().ContainSingle(t => t.Type == TransactionType.Debit && t.Amount.Amount == 400m);

        var txB = await GetTransactionsAsync(accountBId);
        txB.Items.Should().ContainSingle(t => t.Type == TransactionType.Credit && t.Amount.Amount == 400m);
    }

    [Fact]
    public async Task FullFlow_MultipleAccounts_HaveUniqueIbans()
    {
        var ibans = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var accountId = await CreateAccountAsync($"User{i}", "Test");
            var details = await Sut.GetAccountDetailsAsync(
                new GetAccountDetailsQuery(accountId), CancellationToken.None);
            ibans.Add(details.Value?.Iban);
        }

        ibans.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task FullFlow_AllFourOperations_FinalStateCorrectViaQueryMethods()
    {
        var accountAId = await CreateAccountAsync("Alice", "Smith");
        var accountBId = await CreateAccountAsync("Bob", "Jones");

        (await Sut.DepositAsync(
            new DepositCommand(accountAId, new Money(1000, Currency.EUR)), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        (await Sut.TransferAsync(
            new TransferCommand(accountAId, accountBId, new Money(600, Currency.EUR)), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        (await Sut.WithdrawAsync(
            new WithdrawCommand(accountBId, new Money(200, Currency.EUR)), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        (await GetBalanceAsync(accountAId)).Should().Be(400m);
        (await GetBalanceAsync(accountBId)).Should().Be(400m);

        var detailsA = await Sut.GetAccountDetailsAsync(
            new GetAccountDetailsQuery(accountAId), CancellationToken.None);
        detailsA.Value?.FirstName.Should().Be("Alice");

        var detailsB = await Sut.GetAccountDetailsAsync(
            new GetAccountDetailsQuery(accountBId), CancellationToken.None);
        detailsB.Value?.FirstName.Should().Be("Bob");

        var txA = await GetTransactionsAsync(accountAId);
        txA.TotalCount.Should().Be(2);
        txA.Items.Should().ContainSingle(t => t.Type == TransactionType.Credit);
        txA.Items.Should().ContainSingle(t => t.Type == TransactionType.Debit);

        var txB = await GetTransactionsAsync(accountBId);
        txB.TotalCount.Should().Be(2);
        txB.Items.Should().ContainSingle(t => t.Type == TransactionType.Credit);
        txB.Items.Should().ContainSingle(t => t.Type == TransactionType.Debit);
    }
}
