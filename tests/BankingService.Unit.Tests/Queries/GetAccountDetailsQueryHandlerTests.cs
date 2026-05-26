using BankingService.Application.Queries.GetAccountDetails;
using BankingService.Domain.Entities;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using BankingService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Unit.Tests.Queries;

public class GetAccountDetailsQueryHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BankingDbContext _context;
    private readonly GetAccountDetailsQueryHandler _sut;

    public GetAccountDetailsQueryHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BankingDbContext(options);
        _context.Database.EnsureCreated();

        _sut = new GetAccountDetailsQueryHandler(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task HandleAsync_AccountExists_ReturnsAccountDto()
    {
        var account = CreateAccount();
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetAccountDetailsQuery(account.AccountId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value?.AccountId.Should().Be(account.AccountId);
        result.Value?.FirstName.Should().Be(account.FirstName);
        result.Value?.LastName.Should().Be(account.LastName);
        result.Value?.Iban.Should().Be(account.Iban);
        result.Value?.Balance.Amount.Should().Be(account.Balance.Amount);
        result.Value?.Balance.Currency.Should().Be(account.Balance.Currency);
        result.Value?.CreatedAt.Should().Be(account.CreatedAt);
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ReturnsFailure()
    {
        var result = await _sut.HandleAsync(new GetAccountDetailsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == "Account not found.");
    }

    [Fact]
    public async Task HandleAsync_InactiveAccount_ReturnsAccountDto()
    {
        var account = CreateAccount(isActive: false);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetAccountDetailsQuery(account.AccountId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value?.AccountId.Should().Be(account.AccountId);
    }

    private static Account CreateAccount(bool isActive = true) => new()
    {
        AccountId = Guid.NewGuid(),
        FirstName = "Ratko",
        LastName = "Petrović",
        Iban = $"RS35{Guid.NewGuid():N}"[..22],
        Currency = Currency.EUR,
        Balance = new Money(1000m, Currency.EUR),
        IsActive = isActive,
        CreatedAt = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc),
        UpdatedAt = DateTime.UtcNow
    };
}
