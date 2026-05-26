# Testing Conventions

## Test Projects

### BankingService.Unit.Tests
- Tests individual command handlers and query handlers in isolation
- Uses **EF Core with SQLite in-memory** (`Data Source=:memory:`)
- No HTTP, no real file system
- Fast — entire suite runs in seconds

### BankingService.Integration.Tests
- Tests the full stack through `IAccountService`
- Uses a **real SQLite `.db` file** — migrated once per test class, reset between tests via **Respawn**
- Exercises real migrations, real locking, real IBAN generation
- Slower — runs after unit tests in CI

## Unit Test Setup

Each handler test class creates its own in-memory SQLite database:

```csharp
public class CreateAccountCommandHandlerTests : IDisposable
{
    private readonly BankingDbContext _context;
    private readonly CreateAccountCommandHandler _sut;

    public CreateAccountCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _context = new BankingDbContext(options);
        _context.Database.EnsureCreated();

        var ibanGenerator = new IbanGenerator();
        _sut = new CreateAccountCommandHandler(_context, ibanGenerator);
    }

    public void Dispose() => _context.Dispose();
}
```

**Note:** Use `EnsureCreated()` (not `Migrate()`) for unit tests — faster and sufficient.

## Integration Test Setup

Integration tests use **Respawn** to reset database state between tests. The schema is migrated once per test class; Respawn issues `DELETE FROM` on all tables (in FK-safe order) before each test — much faster than dropping and recreating the database.

```csharp
public class AccountServiceIntegrationTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"banking_test_{Guid.NewGuid()}.db");

    private BankingDbContext _context = null!;
    private IAccountService _sut = null!;
    private Respawner _respawner = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}"
            })
            .Build());

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        _context = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        await _context.Database.MigrateAsync();

        _respawner = await Respawner.CreateAsync(
            _context.Database.GetConnectionString()!,
            new RespawnerOptions { DbAdapter = DbAdapter.SQLite });

        _sut = scope.ServiceProvider.GetRequiredService<IAccountService>();
    }

    public async Task DisposeAsync()
    {
        // Reset DB state after each test class
        await _respawner.ResetAsync(_context.Database.GetConnectionString()!);
        await _context.DisposeAsync();

        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
```

**Why Respawn over EnsureDeleted + Migrate:** Respawn clears data without touching the schema — orders of magnitude faster for test suites with many test classes sharing the same DB file.

## Test Naming

Use the pattern: `MethodName_StateUnderTest_ExpectedBehavior`

```csharp
// Good
HandleAsync_WithValidCommand_ReturnsSuccessWithAccountId()
HandleAsync_WithNegativeAmount_ReturnsFailure()
HandleAsync_WithInsufficientFunds_ReturnsFailureWithMessage()
HandleAsync_WithNonExistentAccount_ReturnsFailure()
TransferAsync_WithSameAccountId_ReturnsFailure()

// Bad
TestDeposit()
ShouldWork()
DepositTest1()
```

## What to Test — Unit (Handler Level)

For each **command handler**, test:
- [ ] Happy path — returns `Result.Success` with correct value
- [ ] Domain failure cases — returns `Result.Failure` with descriptive message
- [ ] Account not found — returns `Result.Failure`
- [ ] Side effects — correct `Transaction` record appended to DB
- [ ] Balance update — `Account.Balance` reflects the operation

For each **query handler**, test:
- [ ] Happy path — returns `Result.Success` with correct DTO
- [ ] Not found — returns `Result.Failure`
- [ ] Pagination — correct page, correct item count, correct `TotalCount`
- [ ] Filters — `FromDate`/`ToDate` filter correctly

## What to Test — Integration (IAccountService Level)

- [ ] Full create → deposit → withdraw flow
- [ ] Transfer between two accounts — both balances updated, both transaction logs updated
- [ ] Concurrent deposits do not corrupt balance (run 10 tasks simultaneously)
- [ ] Withdrawal rejected when insufficient funds after concurrent operations
- [ ] IBAN is unique per account

## FluentAssertions Usage

```csharp
// Result assertions
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeEmpty(); // for Guid
result.Value.Should().Be(1500.00m);

result.IsFailure.Should().BeTrue();
result.Errors.Should().ContainSingle()
    .Which.Should().Be("Insufficient funds.");

// Entity assertions
var account = await _context.Accounts.FindAsync(accountId);
account.Should().NotBeNull();
account!.Balance.Amount.Should().Be(1500.00m);

var transactions = await _context.Transactions
    .Where(t => t.AccountId == accountId)
    .ToListAsync();
transactions.Should().HaveCount(2);
```

## What NOT to Test

- FluentValidation rules — these are tested implicitly through the decorator in integration tests; unit testing validator rules directly is low value
- EF Core infrastructure — do not test that EF Core saves correctly; trust the framework
- `AccountService` locking logic in unit tests — this is an integration concern