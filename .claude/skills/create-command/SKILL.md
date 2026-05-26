---
name: create-command
description: Creates a complete CQRS command slice for the BankingService. Use when adding a new operation that mutates state — generates the command record, handler, FluentValidation validator, and unit test.
---

# Create Command

Creates four files for a complete command slice:
- `{CommandName}.cs` — immutable record implementing `ICommand<TResult>`
- `{CommandName}Handler.cs` — handler using `BankingDbContext` directly
- `{CommandName}Validator.cs` — FluentValidation validator
- `{CommandName}HandlerTests.cs` — xUnit unit test with SQLite in-memory

## Before generating, ask

- What does this command return on success? (`Result<Guid>` / `Result<decimal>` / `Result`)
- What are the input properties?
- What is the primary domain rule enforced?

## File templates

### Command
```csharp
using BankingService.Application.Common;
using BankingService.Application.CQRS;

namespace BankingService.Application.Commands.{FeatureFolder};

public record {CommandName}(
    // TODO: properties
) : ICommand<{TResult}>;
```

### Handler
```csharp
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Application.Commands.{FeatureFolder};

public class {CommandName}Handler : ICommandHandler<{CommandName}, {TResult}>
{
    private readonly BankingDbContext _context;

    public {CommandName}Handler(BankingDbContext context) => _context = context;

    public async Task<{TResult}> HandleAsync(
        {CommandName} command, CancellationToken ct, bool saveChanges = true)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == command.AccountId, ct);

        if (account is null)
            return {TResult}.Failure("Account not found.");

        // TODO: domain logic

        if (saveChanges)
            await _context.SaveChangesAsync(ct);

        return {TResult}.Success(/* value */);
    }
}
```

### Validator
```csharp
using FluentValidation;

namespace BankingService.Application.Commands.{FeatureFolder};

public class {CommandName}Validator : AbstractValidator<{CommandName}>
{
    public {CommandName}Validator()
    {
        // TODO: rules — input shape only, no DB queries, no domain rules
    }
}
```

### Unit test
```csharp
using BankingService.Application.Commands.{FeatureFolder};
using BankingService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BankingService.Unit.Tests.Commands.{FeatureFolder};

public class {CommandName}HandlerTests : IDisposable
{
    private readonly BankingDbContext _context;
    private readonly {CommandName}Handler _sut;

    public {CommandName}HandlerTests()
    {
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _context = new BankingDbContext(options);
        _context.Database.EnsureCreated();
        _sut = new {CommandName}Handler(_context);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ReturnsSuccess()
    {
        // Arrange — seed data, build command
        // Act
        // Assert — result, balance, transaction log
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentAccount_ReturnsFailure()
    {
        var result = await _sut.HandleAsync(new {CommandName}(/* invalid id */), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be("Account not found.");
    }

    public void Dispose() => _context.Dispose();
}
```

## Rules

- No locking in handlers — locking is the facade's responsibility
- No validation in handlers — validator runs before handler via decorator
- Always respect the `saveChanges` flag — Transfer calls sub-handlers with `saveChanges: false`
- Return `Result.Failure(message)` for domain errors — never throw
- Use tracked entities for mutations, `AsNoTracking()` for reads only
- Scrutor auto-discovers handlers and validators — no manual DI registration

## Checklist

- [ ] Command record implements `ICommand<{TResult}>` with correct return type
- [ ] Handler returns `Result.Failure(...)` for every domain error path
- [ ] Validator covers all input properties (input shape only, no DB access)
- [ ] At least 3 tests: happy path, account not found, one domain failure
- [ ] `saveChanges` flag respected in handler