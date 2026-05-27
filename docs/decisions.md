# Architecture Decision Records

## ADR-001: SQLite over in-memory storage

**Context:** The task states that in-memory storage is sufficient.

**Decision:** Use SQLite with EF Core instead of a pure in-memory collection.

**Reasons:**
- SQLite provides ACID transaction guarantees, which are critical for transfer atomicity (debit + credit must either both succeed or both fail)
- EF Core's `Database.BeginTransactionAsync()` gives us a real rollback mechanism
- The paginated transaction query (`GetAccountTransactions`) benefits from a real SQL engine (skip/take, date filtering, ordering)
- SQLite is a single file, zero infrastructure — no daemon, no Docker service required
- Migrations demonstrate production-readiness habits without added complexity

**Consequence:** Requires EF Core migration management. `Database.Migrate()` called on startup.

---

## ADR-002: App-level SemaphoreSlim over database-level locks

**Context:** Banking operations on the same account must be serialized. SQLite does not support row-level locking.

**Decision:** Use a `ConcurrentDictionary<Guid, SemaphoreSlim>` per account ID, managed by `AccountLockService`.

**Reasons:**
- SQLite's file-level locking (`BEGIN IMMEDIATE`) would serialize all write operations across all accounts, destroying concurrency
- `SemaphoreSlim(1,1)` per account gives per-account serialization while allowing concurrent operations on different accounts
- Simple, zero-dependency, correct for a single-process deployment

**Scaling path:** For multi-process deployments (horizontal scaling, load balancing), replace `AccountLockService` with a Medallion.Threading implementation using a SQL Server or Redis backend. The `IAccountLockService` interface absorbs this change with zero impact on handlers or the facade.

**Consequence:** Locking is in-process only. A second process instance would bypass these locks. Acceptable for this scope; documented migration path exists.

---

## ADR-003: Locking in the facade, not in handlers

**Context:** Deposit, Withdrawal, and Transfer all require account locking.

**Decision:** All locking is centralized in `AccountService` (the facade implementation).

**Reasons:**
- Transfer requires locking two accounts in a consistent order — this cannot be cleanly encapsulated in a single repository call
- Handlers remain pure business logic with no locking awareness
- Impossible to forget locking on a new operation — it's always the facade's responsibility
- Makes deadlock prevention (ascending lock order) a single, reviewable concern

**Consequence:** Handlers are not thread-safe in isolation — they rely on the facade to serialize access. This is by design and documented.

---

## ADR-004: Result<T> for domain errors, BankingValidationException for validation

**Context:** Operations can fail in two distinct ways: invalid input (wrong format, out of bounds) and domain rule violations (insufficient funds, account not found).

**Decision:**
- `BankingValidationException` for validation failures — thrown by the decorator/dispatcher before the handler runs
- `Result<T>.Failure(...)` for domain errors — returned from handlers

**Reasons:**
- Validation failures are pre-handler — the handler should never see invalid input
- Domain errors are part of the handler's contract — callers must handle them explicitly
- Separates "you sent bad input" (400) from "your request is valid but cannot be fulfilled" (422)
- `Result<T>` makes the failure path visible in method signatures — no hidden exceptions

**Consequence:** Two different error-handling paths must be understood. The `ExceptionMiddleware` handles `BankingValidationException`. Endpoints check `result.IsFailure` for domain errors.

---

## ADR-005: No repository pattern — DbContext directly in handlers

**Context:** Common .NET architecture includes a repository layer between handlers and the DB.

**Decision:** Handlers use `BankingDbContext` directly.

**Reasons:**
- EF Core's `DbContext` is already a unit of work and identity map — wrapping it in a repository adds indirection with no benefit at this scale
- `DbSet<T>` is queryable — handlers can express any query needed without a repository interface
- Removes a layer of abstraction that makes the codebase harder to navigate
- Unit tests use SQLite in-memory, so there is no need to mock a repository interface

**Consequence:** Handlers are coupled to EF Core. If the ORM ever changes, handlers change. Acceptable trade-off for the scope of this project.

---

## ADR-006: Commands as records

**Context:** Commands need to carry data from the API layer to handlers.

**Decision:** All commands are `record` types with positional parameters (immutable by construction).

**Reasons:**
- Immutable commands cannot be mutated after dispatch — no accidental side effects
- Records provide structural equality for free — useful in tests
- Positional parameters make required fields explicit
- No `new()` constraint on `ICommandHandler` — removes the requirement for parameterless constructors

**Consequence:** Commands cannot be used as form-bound model types that require a parameterless constructor. Request DTOs in the API layer are separate classes that map to command records.

---


## ADR-007: Auth is out of scope

**Context:** The task does not mention authentication or authorization.

**Decision:** No auth is implemented. The service trusts the `AccountId` provided in requests.

**Architectural note:** Authentication would plug in at the API layer (JWT bearer middleware). Authorization would be enforced either in the facade (`if (caller.AccountId != command.AccountId) return Forbidden`) or via an ASP.NET Core policy. The `IAccountService` interface would receive a caller identity parameter. This is a one-layer concern that does not affect the Application or Domain layers.

## ADR-008: Cross-currency transfers are not supported — FX extension planned via third-party API + Polly

**Context:** Accounts carry a fixed `Currency`. A transfer between a EUR account and a USD account cannot be fulfilled at face value — the amounts are incommensurable without an exchange rate.

**Decision:** Reject cross-currency transfers at the handler level with a clear error: `"Cross-currency transfers are not supported. Source account currency: {X}, destination account currency: {Y}."` No FX conversion is performed.

**Reasons:**
- Introducing live FX rates requires a third-party API dependency and rate-caching — both out of scope for this iteration
- Failing explicitly with a named error is strictly better than the previous silent failure (withdraw succeeds, deposit fails with a generic currency mismatch message)
- The error surfaces at the start of `TransferCommandHandler`, before any balance mutation, so the source account is never touched

**Extension path — FX support:**
1. Introduce `IFxRateProvider` in the Application layer with a single method `GetRateAsync(Currency from, Currency to, CancellationToken ct) → decimal`
2. Implement against a third-party REST FX API (e.g. exchangerate.host or Fixer.io) in the Infrastructure layer
3. Wrap the HTTP client with **Polly** policies: retry with exponential back-off for transient failures, circuit breaker to shed load during outages, and a fallback to a short-TTL cached rate so transfers degrade gracefully rather than hard-failing
4. `TransferCommandHandler` calls `IFxRateProvider` when currencies differ, converts the debit amount, and records both the original and converted amounts on the transaction
5. The cross-currency guard becomes a feature-flag check rather than a hard rejection — no change to the facade, validator, or `Account` entity

**Consequence:** Until the extension is built, any cross-currency transfer request returns HTTP 422. The error message explicitly names both currencies so callers can adapt without guessing.

---

## ADR-009: TimeProvider injection over DateTime.UtcNow

**Context:** Handlers that record timestamps (`CreateAccount`, `Deposit`, `Withdraw`) were calling `DateTime.UtcNow` directly, making the timestamp untestable and the handlers non-deterministic under test.

**Decision:** Inject `TimeProvider` (built into .NET 8+) into every handler that needs the current time. Resolve `TimeProvider.System` as a singleton from DI in production; tests can substitute a `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` when timestamp assertions are needed.

**Reasons:**
- `TimeProvider` is the BCL-sanctioned abstraction for system time — no custom `IClock` interface required
- Enables deterministic unit tests that control the clock
- `TimeProvider.GetUtcNow()` returns `DateTimeOffset`; `.UtcDateTime` gives the `DateTime` expected by the domain model

**Consequence:** All timestamp-producing handlers take a `TimeProvider` constructor parameter. `TimeProvider.System` is registered as a singleton in `DependencyInjection.AddApplication()`.

---