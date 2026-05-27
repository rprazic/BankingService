using BankingService.Application.Commands.CreateAccount;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using BankingService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BankingService.Unit.Tests.Commands;

public class CreateAccountCommandHandlerTests : BankingDbContextTestBase
{
    private readonly CreateAccountCommandHandler _sut;

    public CreateAccountCommandHandlerTests()
    {
        _sut = new CreateAccountCommandHandler(Context, new IbanGenerator(), CreateDispatcher(),
            NullLogger<CreateAccountCommandHandler>.Instance, TimeProvider);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSuccessResult()
    {
        var command = ValidCommand();

        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_PersistsAccountToDatabase()
    {
        var command = ValidCommand();

        await _sut.HandleAsync(command, CancellationToken.None);

        var account = await Context.Accounts.FirstOrDefaultAsync();
        account.Should().NotBeNull();
        account.FirstName.Should().Be(command.FirstName);
        account.LastName.Should().Be(command.LastName);
        account.Currency.Should().Be(command.InitialDeposit.Currency);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_SetsBalanceToInitialDeposit()
    {
        var command = ValidCommand();

        await _sut.HandleAsync(command, CancellationToken.None);

        var account = await Context.Accounts.FirstOrDefaultAsync();
        account?.Balance.Amount.Should().Be(command.InitialDeposit.Amount);
        account?.Balance.Currency.Should().Be(command.InitialDeposit.Currency);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_SetsIsActiveTrue()
    {
        var command = ValidCommand();

        await _sut.HandleAsync(command, CancellationToken.None);

        var account = await Context.Accounts.FirstOrDefaultAsync();
        account?.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_GeneratesNonEmptyIban()
    {
        var command = ValidCommand();

        await _sut.HandleAsync(command, CancellationToken.None);

        var account = await Context.Accounts.FirstOrDefaultAsync();
        account?.Iban.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task HandleAsync_WithPositiveInitialDeposit_CreatesCreditTransaction()
    {
        var command = ValidCommand();

        await _sut.HandleAsync(command, CancellationToken.None);

        var transaction = await Context.Transactions.FirstOrDefaultAsync();
        transaction.Should().NotBeNull();
        transaction.Type.Should().Be(TransactionType.Credit);
        transaction.Amount.Amount.Should().Be(command.InitialDeposit.Amount);
        transaction.Description.Should().Be("Initial deposit");
    }

    [Fact]
    public async Task HandleAsync_WithZeroInitialDeposit_DoesNotCreateTransaction()
    {
        var command = new CreateAccountCommand("Ratko", "Prazic", new Money(0m, Currency.EUR));

        await _sut.HandleAsync(command, CancellationToken.None);

        var count = await Context.Transactions.CountAsync();
        count.Should().Be(0);
    }

    private static CreateAccountCommand ValidCommand() =>
        new("Ratko", "Prazic", new Money(1000m, Currency.EUR));
}