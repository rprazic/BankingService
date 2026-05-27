using BankingService.Application.Commands.CreateTransaction;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Domain.Entities;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using BankingService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingService.Unit.Tests;

public abstract class BankingDbContextTestBase : IDisposable
{
    private readonly SqliteConnection _connection;

    protected readonly BankingDbContext Context;

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
        services.AddSingleton(Context);
        services.AddScoped<ICommandHandler<CreateTransactionCommand, Result<Guid>>, CreateTransactionCommandHandler>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        return services.BuildServiceProvider().GetRequiredService<ICommandDispatcher>();
    }

    protected static Account CreateAccount(decimal balance = 1000m, bool isActive = true, string firstName = "Test",
        string lastName = "User") => new()
    {
        AccountId = Guid.NewGuid(),
        FirstName = firstName,
        LastName = lastName,
        Iban = $"RS35{Guid.NewGuid():N}"[..22],
        Currency = Currency.EUR,
        Balance = new Money(balance, Currency.EUR),
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
