using BankingService.Application.Common;
using BankingService.Application.DTOs;
using BankingService.Application.Queries.GetAccountTransactions;
using BankingService.Domain.Entities;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using BankingService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Unit.Tests.Queries;

public class GetAccountTransactionsQueryHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BankingDbContext _context;
    private readonly GetAccountTransactionsQueryHandler _sut;

    public GetAccountTransactionsQueryHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BankingDbContext(options);
        _context.Database.EnsureCreated();

        _sut = new GetAccountTransactionsQueryHandler(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task HandleAsync_AccountHasTransactions_ReturnsPaginatedResult()
    {
        var account = CreateAccount();
        _context.Accounts.Add(account);
        _context.Transactions.AddRange(
            CreateTransaction(account.AccountId, TransactionType.Credit, 500m),
            CreateTransaction(account.AccountId, TransactionType.Debit, 200m)
        );
        await _context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetAccountTransactionsQuery(account.AccountId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value?.TotalCount.Should().Be(2);
        result.Value?.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_NoTransactionsExist_ReturnsEmptyPagedResult()
    {
        var account = CreateAccount();
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetAccountTransactionsQuery(account.AccountId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value?.TotalCount.Should().Be(0);
        result.Value?.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ReturnsFailure()
    {
        var result = await _sut.HandleAsync(new GetAccountTransactionsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == "Account not found.");
    }

    [Fact]
    public async Task HandleAsync_FromDateFilter_ExcludesOlderTransactions()
    {
        var account = CreateAccount();
        _context.Accounts.Add(account);
        var cutoff = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        _context.Transactions.AddRange(
            CreateTransaction(account.AccountId, TransactionType.Credit, 100m, cutoff.AddDays(-1)),
            CreateTransaction(account.AccountId, TransactionType.Credit, 200m, cutoff.AddDays(1))
        );
        await _context.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new GetAccountTransactionsQuery(account.AccountId, FromDate: cutoff),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value?.TotalCount.Should().Be(1);
        result.Value?.Items[0].Amount.Should().Be(200m);
    }

    [Fact]
    public async Task HandleAsync_ToDateFilter_ExcludesNewerTransactions()
    {
        var account = CreateAccount();
        _context.Accounts.Add(account);
        var cutoff = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        _context.Transactions.AddRange(
            CreateTransaction(account.AccountId, TransactionType.Credit, 100m, cutoff.AddDays(-1)),
            CreateTransaction(account.AccountId, TransactionType.Credit, 200m, cutoff.AddDays(1))
        );
        await _context.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new GetAccountTransactionsQuery(account.AccountId, ToDate: cutoff),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value?.TotalCount.Should().Be(1);
        result.Value?.Items[0].Amount.Should().Be(100m);
    }

    [Fact]
    public async Task HandleAsync_BothDateFilters_ReturnsTransactionsInRange()
    {
        var account = CreateAccount();
        _context.Accounts.Add(account);
        var from = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        _context.Transactions.AddRange(
            CreateTransaction(account.AccountId, TransactionType.Credit, 50m, from.AddDays(-1)),
            CreateTransaction(account.AccountId, TransactionType.Credit, 100m, from.AddDays(5)),
            CreateTransaction(account.AccountId, TransactionType.Credit, 200m, to.AddDays(1))
        );
        await _context.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new GetAccountTransactionsQuery(account.AccountId, FromDate: from, ToDate: to),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value?.TotalCount.Should().Be(1);
        result.Value?.Items[0].Amount.Should().Be(100m);
    }

    [Fact]
    public async Task HandleAsync_ResultsOrderedByCreatedAtDescending()
    {
        var account = CreateAccount();
        _context.Accounts.Add(account);
        var date = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        _context.Transactions.AddRange(
            CreateTransaction(account.AccountId, TransactionType.Credit, 100m, date),
            CreateTransaction(account.AccountId, TransactionType.Credit, 200m, date.AddDays(1)),
            CreateTransaction(account.AccountId, TransactionType.Credit, 300m, date.AddDays(2))
        );
        await _context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetAccountTransactionsQuery(account.AccountId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value?.Items.Select(t => t.Amount).Should().ContainInOrder(300m, 200m, 100m);
    }

    [Fact]
    public async Task HandleAsync_PaginationRespected_ReturnsCorrectPage()
    {
        var account = CreateAccount();
        _context.Accounts.Add(account);
        var date = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 5; i++)
        {
            _context.Transactions.Add(
                CreateTransaction(account.AccountId, TransactionType.Credit, (i + 1) * 100m, date.AddDays(i)));
        }

        await _context.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new GetAccountTransactionsQuery(account.AccountId, Page: 2, PageSize: 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value?.TotalCount.Should().Be(5);
        result.Value?.Items.Should().HaveCount(2);
        result.Value?.Page.Should().Be(2);
    }

    private static Account CreateAccount() => new()
    {
        AccountId = Guid.NewGuid(),
        FirstName = "Test",
        LastName = "User",
        Iban = $"RS35{Guid.NewGuid():N}"[..22],
        Currency = Currency.EUR,
        Balance = new Money(1000m, Currency.EUR),
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Transaction CreateTransaction(
        Guid accountId,
        TransactionType type,
        decimal amount,
        DateTime? createdAt = null) => new()
    {
        TransactionId = Guid.NewGuid(),
        AccountId = accountId,
        Type = type,
        Amount = new Money(amount, Currency.EUR),
        CreatedAt = createdAt ?? DateTime.UtcNow
    };
}