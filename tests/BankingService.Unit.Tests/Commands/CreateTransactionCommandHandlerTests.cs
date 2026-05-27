using BankingService.Application.Commands.CreateTransaction;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Unit.Tests.Commands;

public class CreateTransactionCommandHandlerTests : BankingDbContextTestBase
{
    private readonly CreateTransactionCommandHandler _sut;

    public CreateTransactionCommandHandlerTests()
    {
        _sut = new CreateTransactionCommandHandler(Context);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSuccessResult()
    {
        var account = CreateAccount();
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(ValidCommand(account.AccountId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_PersistsTransactionToDatabase()
    {
        var account = CreateAccount();
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();
        var command = ValidCommand(account.AccountId);

        await _sut.HandleAsync(command, CancellationToken.None);

        var transaction = await Context.Transactions.FirstOrDefaultAsync();
        transaction.Should().NotBeNull();
        transaction.AccountId.Should().Be(command.AccountId);
        transaction.Type.Should().Be(command.Type);
        transaction.Amount.Amount.Should().Be(command.Amount.Amount);
        transaction.Amount.Currency.Should().Be(command.Amount.Currency);
        transaction.Description.Should().Be(command.Description);
        transaction.CreatedAt.Should().Be(command.CreatedAt);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsTransactionId()
    {
        var account = CreateAccount();
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(ValidCommand(account.AccountId), CancellationToken.None);

        var transaction = await Context.Transactions.FirstOrDefaultAsync();
        result.Value.Should().Be(transaction!.TransactionId);
    }

    [Fact]
    public async Task HandleAsync_WithRelatedAccountId_PersistsRelatedAccountId()
    {
        var account = CreateAccount();
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();
        var relatedId = Guid.NewGuid();
        var command = ValidCommand(account.AccountId) with { RelatedAccountId = relatedId };

        await _sut.HandleAsync(command, CancellationToken.None);

        var transaction = await Context.Transactions.FirstOrDefaultAsync();
        transaction?.RelatedAccountId.Should().Be(relatedId);
    }

    [Fact]
    public async Task HandleAsync_SaveChangesFalse_DoesNotPersistTransaction()
    {
        var account = CreateAccount();
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        await _sut.HandleAsync(ValidCommand(account.AccountId), CancellationToken.None, saveChanges: false);

        var count = await Context.Transactions.CountAsync();
        count.Should().Be(0);
    }

    private static CreateTransactionCommand ValidCommand(Guid accountId) => new(
        AccountId: accountId,
        Type: TransactionType.Credit,
        Amount: new Money(100m, Currency.EUR),
        CreatedAt: DateTime.UtcNow,
        Description: "Test transaction"
    );
}