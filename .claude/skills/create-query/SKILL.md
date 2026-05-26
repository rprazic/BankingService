---
name: create-query
description: Creates a complete CQRS query slice for the BankingService. Use when adding a new read operation — generates the query record, handler, optional validator, and unit test. Supports both single-result and paginated variants.
---

# Create Query

Creates files for a complete query slice:
- `{QueryName}.cs` — immutable record implementing `IQuery<Result<TDto>>`
- `{QueryName}Handler.cs` — read-only handler using `AsNoTracking()`
- `{QueryName}Validator.cs` — optional, only when structural validation is needed
- `{QueryName}HandlerTests.cs` — xUnit unit test with SQLite in-memory

## Before generating, ask

- What does this query return? (`Result<AccountDto>` / `Result<PagedResult<TransactionDto>>` / etc.)
- Is this paginated? If yes, what filter parameters?
- What are the input parameters?

## File templates

### Query — standard
```csharp
using BankingService.Application.Common;
using BankingService.Application.CQRS;

namespace BankingService.Application.Queries.{FeatureFolder};

public record {QueryName}(Guid AccountId) : IQuery<Result<{TDto}>>;
```

### Query — paginated
```csharp
using BankingService.Application.Common;
using BankingService.Application.CQRS;

namespace BankingService.Application.Queries.{FeatureFolder};

public record {QueryName}(
    Guid AccountId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20
) : PagedQuery(Page, PageSize), IQuery<Result<PagedResult<{TDto}>>>;
```

### Handler — standard
```csharp
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Application.Queries.{FeatureFolder};

public class {QueryName}Handler : IQueryHandler<{QueryName}, Result<{TDto}>>
{
    private readonly BankingDbContext _context;

    public {QueryName}Handler(BankingDbContext context) => _context = context;

    public async Task<Result<{TDto}>> HandleAsync({QueryName} query, CancellationToken ct)
    {
        var entity = await _context.{DbSet}
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountId == query.AccountId, ct);

        if (entity is null)
            return Result<{TDto}>.Failure("{Entity} not found.");

        return Result<{TDto}>.Success(new {TDto}(/* map properties */));
    }
}
```

### Handler — paginated
```csharp
public async Task<Result<PagedResult<{TDto}>>> HandleAsync({QueryName} query, CancellationToken ct)
{
    var dbQuery = _context.{DbSet}
        .AsNoTracking()
        .Where(x => x.AccountId == query.AccountId);

    if (query.FromDate.HasValue)
        dbQuery = dbQuery.Where(x => x.CreatedAt >= query.FromDate.Value);

    if (query.ToDate.HasValue)
        dbQuery = dbQuery.Where(x => x.CreatedAt <= query.ToDate.Value);

    var totalCount = await dbQuery.CountAsync(ct);
    var items = await dbQuery
        .OrderByDescending(x => x.CreatedAt)
        .Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize)
        .Select(x => new {TDto}(/* map */))
        .ToListAsync(ct);

    return Result<PagedResult<{TDto}>>.Success(
        new PagedResult<{TDto}>(items, query.Page, query.PageSize, totalCount));
}
```

### Validator (only if structural validation is needed)
```csharp
using FluentValidation;

namespace BankingService.Application.Queries.{FeatureFolder};

public class {QueryName}Validator : AbstractValidator<{QueryName}>
{
    public {QueryName}Validator()
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("Account ID is required.");
        // For paginated: page > 0, pageSize between 1 and 100
    }
}
```

### Unit test
```csharp
using BankingService.Application.Queries.{FeatureFolder};
using BankingService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BankingService.Unit.Tests.Queries.{FeatureFolder};

public class {QueryName}HandlerTests : IDisposable
{
    private readonly BankingDbContext _context;
    private readonly {QueryName}Handler _sut;

    public {QueryName}HandlerTests()
    {
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _context = new BankingDbContext(options);
        _context.Database.EnsureCreated();
        _sut = new {QueryName}Handler(_context);
    }

    [Fact]
    public async Task HandleAsync_WithExistingAccount_ReturnsSuccess()
    {
        // Arrange — seed data
        // Act
        // Assert — DTO properties, pagination metadata
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentAccount_ReturnsFailure()
    {
        var result = await _sut.HandleAsync(new {QueryName}(Guid.NewGuid()), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
    }

    public void Dispose() => _context.Dispose();
}
```

## Rules

- Always use `AsNoTracking()` — queries never mutate state
- Return `Result.Failure(...)` when entity not found
- Validator is optional — only create when structural constraints exist
- QueryDispatcher runs validators automatically if registered
- Scrutor auto-discovers handlers and validators — no manual DI registration

## Checklist

- [ ] Query record implements `IQuery<Result<{TDto}>>` with correct return type
- [ ] Handler uses `AsNoTracking()` throughout
- [ ] Paginated handler uses `CountAsync` + `Skip/Take`
- [ ] At least 2 tests: found, not found
- [ ] Paginated queries have additional filter and pagination tests