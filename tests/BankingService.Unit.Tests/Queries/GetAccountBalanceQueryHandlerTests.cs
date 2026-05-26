using BankingService.Application.Queries.GetAccountBalance;
using BankingService.Domain.Entities;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using BankingService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Unit.Tests.Queries;

public class GetAccountBalanceQueryHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BankingDbContext _context;
    private readonly GetAccountBalanceQueryHandler _sut;

    public GetAccountBalanceQueryHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BankingDbContext(options);
        _context.Database.EnsureCreated();

        _sut = new GetAccountBalanceQueryHandler(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task HandleAsync_AccountExists_ReturnsBalanceDto()
    {
        var account = CreateAccount(balance: 2500m);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetAccountBalanceQuery(account.AccountId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccountId.Should().Be(account.AccountId);
        result.Value.Balance.Should().Be(2500m);
        result.Value.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ReturnsFailure()
    {
        var result = await _sut.HandleAsync(new GetAccountBalanceQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == "Account not found.");
    }

    private static Account CreateAccount(decimal balance = 1000m) => new()
    {
        AccountId = Guid.NewGuid(),
        FirstName = "Test",
        LastName = "User",
        Iban = $"RS35{Guid.NewGuid():N}"[..22],
        Currency = Currency.EUR,
        Balance = new Money(balance, Currency.EUR),
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
