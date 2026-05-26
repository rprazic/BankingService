using BankingService.Application.Commands.CreateAccount;
using BankingService.Domain.Enums;
using BankingService.Infrastructure.Persistence;
using BankingService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Unit.Tests.Commands;

public class CreateAccountCommandHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BankingDbContext _context;
    private readonly CreateAccountCommandHandler _sut;

    public CreateAccountCommandHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BankingDbContext(options);
        _context.Database.EnsureCreated();

        _sut = new CreateAccountCommandHandler(_context, new IbanGenerator());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
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

        var account = await _context.Accounts.FirstOrDefaultAsync();
        account.Should().NotBeNull();
        account.FirstName.Should().Be(command.FirstName);
        account.LastName.Should().Be(command.LastName);
        account.Currency.Should().Be(command.Currency);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_SetsBalanceToInitialDeposit()
    {
        var command = ValidCommand();

        await _sut.HandleAsync(command, CancellationToken.None);

        var account = await _context.Accounts.FirstOrDefaultAsync();
        account?.Balance.Amount.Should().Be(command.InitialDeposit);
        account?.Balance.Currency.Should().Be(command.Currency);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_SetsIsActiveTrue()
    {
        var command = ValidCommand();

        await _sut.HandleAsync(command, CancellationToken.None);

        var account = await _context.Accounts.FirstOrDefaultAsync();
        account?.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_GeneratesNonEmptyIban()
    {
        var command = ValidCommand();

        await _sut.HandleAsync(command, CancellationToken.None);

        var account = await _context.Accounts.FirstOrDefaultAsync();
        account?.Iban.Should().NotBeNullOrWhiteSpace();
    }

    private static CreateAccountCommand ValidCommand() =>
        new("Ratko", "Prazic", 1000m, Currency.EUR);
}
