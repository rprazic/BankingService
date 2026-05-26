# Architecture

## Solution Structure

```
src/
├── BankingService.Domain/
│   ├── Entities/
│   │   ├── Account.cs
│   │   └── Transaction.cs
│   ├── ValueObjects/
│   │   ├── Money.cs
│   │   └── BaseValueObject.cs
│   └── Enums/
│       ├── Currency.cs
│       └── TransactionType.cs
│
├── BankingService.Application/
│   ├── Common/
│   │   ├── Result.cs
│   │   ├── ResultT.cs
│   │   ├── PagedResult.cs
│   │   ├── PagedQuery.cs
│   │   └── BankingValidationException.cs
│   ├── CQRS/
│   │   ├── ICommand.cs
│   │   ├── ICommandHandler.cs
│   │   ├── ICommandDispatcher.cs
│   │   ├── CommandDispatcher.cs
│   │   ├── IQuery.cs
│   │   ├── IQueryHandler.cs
│   │   ├── IQueryDispatcher.cs
│   │   └── QueryDispatcher.cs
│   ├── Behaviours/
│   │   └── ValidationCommandHandlerDecorator.cs
│   ├── Commands/
│   │   ├── CreateAccount/
│   │   │   ├── CreateAccountCommand.cs
│   │   │   ├── CreateAccountCommandHandler.cs
│   │   │   └── CreateAccountCommandValidator.cs
│   │   ├── Deposit/
│   │   ├── Withdraw/
│   │   └── Transfer/
│   ├── Queries/
│   │   ├── GetAccountDetails/
│   │   ├── GetAccountBalance/
│   │   └── GetAccountTransactions/
│   ├── DTOs/
│   │   ├── AccountDto.cs
│   │   ├── AccountBalanceDto.cs
│   │   └── TransactionDto.cs
│   ├── AccountService.cs
│   ├── DependencyInjection.cs
│   └── IAccountService.cs
│
├── BankingService.Infrastructure/
│   ├── Persistence/
│   │   ├── BankingDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── AccountConfiguration.cs
│   │   │   └── TransactionConfiguration.cs
│   │   └── Migrations/
│   ├── Locking/
│   │   ├── IAccountLockService.cs        ← canonical interface definition
│   │   └── AccountLockService.cs
│   ├── Services/
│   │   ├── IIbanGenerator.cs             ← canonical interface definition
│   │   └── IbanGenerator.cs
│   └── DependencyInjection.cs
│
└── BankingService.Api/
    ├── Endpoints/
    │   └── AccountEndpoints.cs
    ├── Extensions/
    │   └── DatabaseExtensions.cs
    ├── Middleware/
    │   └── ExceptionMiddleware.cs
    └── Program.cs
```

## Dependency Rules

```
Domain ──────► Infrastructure ──────► Application ──────► Api
```

- **Domain**: zero dependencies on other projects
- **Infrastructure**: depends on Domain only — persistence, locking, IBAN generation
- **Application**: depends on Infrastructure (and transitively Domain) — CQRS handlers, validators, facade, DI wiring
- **Api**: depends on Application only (Infrastructure is transitive)

## Locking Strategy

Locking is the **exclusive responsibility of `AccountService`** (the facade implementation).
Handlers contain no locking code whatsoever.

### Single-account operations (Deposit, Withdraw)
```csharp
await using var accountLock = await _lockService.AcquireAsync(command.AccountId, ct);
return await _commandDispatcher.DispatchAsync<TCommand, TResult>(command, ct);
```

### Transfer — two accounts, deadlock prevention
Always acquire locks in **ascending `AccountId` order** to prevent circular wait:
```csharp
var (firstId, secondId) = command.FromAccountId < command.ToAccountId
    ? (command.FromAccountId, command.ToAccountId)
    : (command.ToAccountId, command.FromAccountId);

await using var lock1 = await _lockService.AcquireAsync(firstId, ct);
await using var lock2 = await _lockService.AcquireAsync(secondId, ct);
return await _commandDispatcher.DispatchAsync<TransferCommand, Result>(command, ct);
```

### IAccountLockService contract
```csharp
public interface IAccountLockService
{
    Task<IAsyncDisposable> AcquireAsync(Guid accountId, CancellationToken ct);
}
```

Implementation (`AccountLockService`) uses `ConcurrentDictionary<Guid, SemaphoreSlim>` with `SemaphoreSlim(1, 1)` per account.

**Scaling note**: For multi-process deployments, replace `AccountLockService` with a Medallion.Threading implementation (SQL Server or Redis backend). The `IAccountLockService` interface absorbs this change with zero impact on handlers or the facade.

## EF Core Configuration

- `Money` VO mapped with `OwnsOne` in both `AccountConfiguration` and `TransactionConfiguration`
- `IBAN` column has a unique index
- `TransactionType` stored as string for readability in the DB
- `Currency` stored as string
- `Transaction.RelatedAccountId` is nullable (only set for transfers)
- Auto-migrate on startup via `DatabaseExtensions.MigrateDatabaseAsync()` — lives in Api project, extends `IHost`

### Money mapping example
```csharp
builder.OwnsOne(a => a.Balance, money =>
{
    money.Property(m => m.Amount).HasColumnName("BalanceAmount").HasPrecision(18, 4);
    money.Property(m => m.Currency).HasColumnName("BalanceCurrency").HasConversion<string>();
});
```

## DI Registration

Application exposes `AddApplication` — the single call from `Program.cs`:
```csharp
builder.Services.AddApplication(builder.Configuration);
```

`AddApplication` (in `Application/DependencyInjection.cs`) owns all registration and calls `AddInfrastructure` internally:
1. Calls `AddInfrastructure` → registers `BankingDbContext` with SQLite connection string
2. Register `IAccountLockService` → `AccountLockService` as singleton
3. Register `IIbanGenerator` → `IbanGenerator` as singleton
4. Register `ICommandDispatcher` → `CommandDispatcher` as scoped
5. Register `IQueryDispatcher` → `QueryDispatcher` as scoped
6. Scrutor scan Application assembly for `ICommandHandler<,>` and `IQueryHandler<,>` → scoped
7. Scrutor decorate all `ICommandHandler<,>` with `ValidationCommandHandlerDecorator<,>`
8. FluentValidation: scan Application assembly for all validators
9. Register `IAccountService` → `AccountService` as scoped

```csharp
services.Scan(selector => selector
    .FromAssemblyOf<ICommandDispatcher>()
    .AddClasses(f => f.AssignableToAny(typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

// Guard prevents Scrutor throwing when no handlers are registered yet
if (services.Any(s => s.ServiceType.IsGenericType &&
    s.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) &&
    s.ImplementationType != null &&
    !s.ImplementationType.IsGenericTypeDefinition))
{
    services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));
}

services.AddValidatorsFromAssemblyContaining<ICommandDispatcher>();
services.AddScoped<IAccountService, AccountService>();
```

`Infrastructure/DependencyInjection.cs` (`AddInfrastructure`) only registers `BankingDbContext` — nothing else.

## Tech Stack

| Concern | Library |
|---|---|
| ORM | EF Core 9 |
| Database | SQLite |
| Validation | FluentValidation |
| DI scanning & decoration | Scrutor |
| API documentation | Microsoft.AspNetCore.OpenApi + Scalar |
| Testing | xUnit + FluentAssertions + Respawn |