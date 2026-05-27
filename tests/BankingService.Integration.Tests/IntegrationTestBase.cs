using BankingService.Application;
using BankingService.Application.Commands.CreateAccount;
using BankingService.Application.Common;
using BankingService.Application.DTOs;
using BankingService.Application.Queries.GetAccountBalance;
using BankingService.Application.Queries.GetAccountTransactions;
using BankingService.Application.Services;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using BankingService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Respawn;

namespace BankingService.Integration.Tests;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"banking_test_{Guid.NewGuid()}.db");

    private BankingDbContext _context = null!;
    private Respawner _respawner = null!;
    private SqliteConnection _respawnConnection = null!;

    protected IAccountService Sut { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var connectionString = $"Data Source={_dbPath}";

        var services = new ServiceCollection();
        services.AddApplication(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build());

        var scope = services.BuildServiceProvider().CreateScope();

        _context = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        await _context.Database.MigrateAsync();

        _respawnConnection = new SqliteConnection(connectionString);
        await _respawnConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(
            _respawnConnection,
            new RespawnerOptions { DbAdapter = DbAdapter.Sqlite });

        Sut = scope.ServiceProvider.GetRequiredService<IAccountService>();
    }

    public async Task DisposeAsync()
    {
        await _respawner.ResetAsync(_respawnConnection);
        await _respawnConnection.DisposeAsync();
        await _context.DisposeAsync();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    protected async Task<Guid> CreateAccountAsync(string firstName, string lastName, decimal initialDeposit = 0)
    {
        var result = await Sut.CreateAccountAsync(
            new CreateAccountCommand(firstName, lastName, new Money(initialDeposit, Currency.EUR)),
            CancellationToken.None);
        return result.Value!;
    }

    protected async Task<decimal> GetBalanceAsync(Guid accountId)
    {
        var result = await Sut.GetAccountBalanceAsync(
            new GetAccountBalanceQuery(accountId), CancellationToken.None);
        return result.Value!.Balance.Amount;
    }

    protected async Task<PagedResult<TransactionDto>> GetTransactionsAsync(Guid accountId)
    {
        var result = await Sut.GetAccountTransactionsAsync(
            new GetAccountTransactionsQuery(accountId), CancellationToken.None);
        return result.Value!;
    }
}
