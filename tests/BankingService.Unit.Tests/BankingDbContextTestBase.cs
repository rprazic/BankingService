using BankingService.Application.Commands.CreateTransaction;
using BankingService.Application.Commands.Deposit;
using BankingService.Application.Commands.Transfer;
using BankingService.Application.Commands.Withdraw;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Domain.Entities;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using BankingService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace BankingService.Unit.Tests;

public abstract class BankingDbContextTestBase : IDisposable
{
    private readonly SqliteConnection _connection;

    protected readonly BankingDbContext Context;
    protected readonly FakeTimeProvider TimeProvider = new();

    protected BankingDbContextTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new BankingDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }

    protected ICommandDispatcher CreateDispatcher()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<TimeProvider>(TimeProvider);
        services.AddSingleton(Context);
        services.AddScoped<ICommandHandler<CreateTransactionCommand, Result<Guid>>, CreateTransactionCommandHandler>();
        services.AddScoped<ICommandHandler<DepositCommand, Result<MoneyDto>>, DepositCommandHandler>();
        services.AddScoped<ICommandHandler<WithdrawCommand, Result<MoneyDto>>, WithdrawCommandHandler>();
        services.AddScoped<ICommandHandler<TransferCommand, Result>, TransferCommandHandler>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ICommandDispatcher>();
    }

    protected Account CreateAccount(decimal balance = 1000m, bool isActive = true, string firstName = "Test",
        string lastName = "User", Currency currency = Currency.EUR)
    {
        var now = TimeProvider.GetUtcNow().UtcDateTime;
        return new Account
        {
            AccountId = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Iban = $"RS35{Guid.NewGuid():N}"[..22],
            Currency = currency,
            Balance = new Money(balance, currency),
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}