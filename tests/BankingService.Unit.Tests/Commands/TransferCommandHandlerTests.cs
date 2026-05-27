using BankingService.Application.Commands.CreateTransaction;
using BankingService.Application.Commands.Deposit;
using BankingService.Application.Commands.Transfer;
using BankingService.Application.Commands.Withdraw;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransactionType = BankingService.Domain.Enums.TransactionType;

namespace BankingService.Unit.Tests.Commands;

public class TransferCommandHandlerTests : BankingDbContextTestBase
{
    private readonly TransferCommandHandler _sut;

    public TransferCommandHandlerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Context);
        services.AddScoped<ICommandHandler<CreateTransactionCommand, Result<Guid>>, CreateTransactionCommandHandler>();
        services.AddScoped<ICommandHandler<DepositCommand, Result<MoneyDto>>, DepositCommandHandler>();
        services.AddScoped<ICommandHandler<WithdrawCommand, Result<MoneyDto>>, WithdrawCommandHandler>();
        services.AddScoped<ICommandHandler<TransferCommand, Result>, TransferCommandHandler>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        var dispatcher = services.BuildServiceProvider().GetRequiredService<ICommandDispatcher>();

        _sut = new TransferCommandHandler(Context, dispatcher);
    }

    [Fact]
    public async Task HandleAsync_ValidTransfer_ReturnsSuccessResult()
    {
        var source = CreateAccount(balance: 1000m);
        var destination = CreateAccount(balance: 500m);
        Context.Accounts.AddRange(source, destination);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new TransferCommand(source.AccountId, destination.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ValidTransfer_DecreasesSourceBalance()
    {
        var source = CreateAccount(balance: 1000m);
        var destination = CreateAccount(balance: 500m);
        Context.Accounts.AddRange(source, destination);
        await Context.SaveChangesAsync();

        await _sut.HandleAsync(
            new TransferCommand(source.AccountId, destination.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        var updated = await Context.Accounts.FindAsync(source.AccountId);
        updated!.Balance.Amount.Should().Be(800m);
    }

    [Fact]
    public async Task HandleAsync_ValidTransfer_IncreasesDestinationBalance()
    {
        var source = CreateAccount(balance: 1000m);
        var destination = CreateAccount(balance: 500m);
        Context.Accounts.AddRange(source, destination);
        await Context.SaveChangesAsync();

        await _sut.HandleAsync(
            new TransferCommand(source.AccountId, destination.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        var updated = await Context.Accounts.FindAsync(destination.AccountId);
        updated!.Balance.Amount.Should().Be(700m);
    }

    [Fact]
    public async Task HandleAsync_ValidTransfer_CreatesDebitTransactionOnSource()
    {
        var source = CreateAccount(balance: 1000m);
        var destination = CreateAccount(balance: 500m);
        Context.Accounts.AddRange(source, destination);
        await Context.SaveChangesAsync();

        await _sut.HandleAsync(
            new TransferCommand(source.AccountId, destination.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        var debit = await Context.Transactions
            .FirstOrDefaultAsync(t => t.AccountId == source.AccountId);
        debit.Should().NotBeNull();
        debit!.Type.Should().Be(TransactionType.Debit);
        debit.Amount.Amount.Should().Be(200m);
    }

    [Fact]
    public async Task HandleAsync_ValidTransfer_CreatesCreditTransactionOnDestination()
    {
        var source = CreateAccount(balance: 1000m);
        var destination = CreateAccount(balance: 500m);
        Context.Accounts.AddRange(source, destination);
        await Context.SaveChangesAsync();

        await _sut.HandleAsync(
            new TransferCommand(source.AccountId, destination.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        var credit = await Context.Transactions
            .FirstOrDefaultAsync(t => t.AccountId == destination.AccountId);
        credit.Should().NotBeNull();
        credit!.Type.Should().Be(TransactionType.Credit);
        credit.Amount.Amount.Should().Be(200m);
    }

    [Fact]
    public async Task HandleAsync_SourceAccountNotFound_ReturnsFailure()
    {
        var destination = CreateAccount(balance: 500m);
        Context.Accounts.Add(destination);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new TransferCommand(Guid.NewGuid(), destination.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Account not found.");
    }

    [Fact]
    public async Task HandleAsync_DestinationAccountNotFound_ReturnsFailure()
    {
        var source = CreateAccount(balance: 1000m);
        Context.Accounts.Add(source);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new TransferCommand(source.AccountId, Guid.NewGuid(), new Money(200m, Currency.EUR)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Account not found.");
    }

    [Fact]
    public async Task HandleAsync_SourceAccountInactive_ReturnsFailure()
    {
        var source = CreateAccount(balance: 1000m, isActive: false);
        var destination = CreateAccount(balance: 500m);
        Context.Accounts.AddRange(source, destination);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new TransferCommand(source.AccountId, destination.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Account is not active.");
    }

    [Fact]
    public async Task HandleAsync_DestinationAccountInactive_ReturnsFailure()
    {
        var source = CreateAccount(balance: 1000m);
        var destination = CreateAccount(balance: 500m, isActive: false);
        Context.Accounts.AddRange(source, destination);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new TransferCommand(source.AccountId, destination.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Account is not active.");
    }

    [Fact]
    public async Task HandleAsync_CurrencyMismatch_ReturnsFailure()
    {
        var source = CreateAccount(balance: 1000m); // EUR
        var destination = CreateAccount(balance: 500m); // EUR
        Context.Accounts.AddRange(source, destination);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new TransferCommand(source.AccountId, destination.AccountId, new Money(200m, Currency.USD)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_InsufficientFunds_ReturnsFailure()
    {
        var source = CreateAccount(balance: 100m);
        var destination = CreateAccount(balance: 500m);
        Context.Accounts.AddRange(source, destination);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new TransferCommand(source.AccountId, destination.AccountId, new Money(200m, Currency.EUR)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Insufficient funds.");
    }
}
