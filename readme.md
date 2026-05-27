# BankingService

A .NET 10 banking service implementing core account operations: account creation, deposits, withdrawals, and transfers between accounts.

Built with Clean Architecture, CQRS, and a Result-based error handling strategy — no MediatR, no repositories.

## Getting Started

### Prerequisites
- .NET 10 SDK
- No additional infrastructure required — SQLite is used as the database

### Run the API
```bash
dotnet run --project src/BankingService.Api
```

API explorer (Scalar) is available at `http://localhost:[port]/scalar/v1`.

### Run Tests
```bash
# Unit tests
dotnet test tests/BankingService.Unit.Tests

# Integration tests
dotnet test tests/BankingService.Integration.Tests

# All tests
dotnet test
```

### Docker
```bash
# Build and run from solution root
docker build -f src/BankingService.Api/Dockerfile -t banking-service .
docker run -p 8080:8080 -v banking_data:/app/data banking-service
```

## Features

| Operation | Description |
|---|---|
| Create account | Opens a new account with an auto-generated IBAN and optional initial deposit |
| Deposit | Credits funds to an account |
| Withdraw | Debits funds with insufficient-balance protection |
| Transfer | Moves funds between two accounts atomically, with deadlock-safe locking |
| Get account | Returns current balance and account metadata |
| Transaction history | Paginated log with optional date range filter |

## Project Structure

```
src/
├── BankingService.Domain/         # Entities, value objects, enums — zero dependencies
├── BankingService.Application/    # CQRS infrastructure, commands, queries, service facade
├── BankingService.Infrastructure/ # EF Core, SQLite, per-account locking, IBAN generation
└── BankingService.Api/           # Minimal API endpoints, middleware, Scalar
tests/
├── BankingService.Unit.Tests/     # Handler-level tests, SQLite in-memory
└── BankingService.Integration.Tests/ # IAccountService-level tests, real SQLite
```

Dependency direction: `Api → Application → Infrastructure → Domain` (Api also references Infrastructure directly for DI registration).

## Tech Stack

| Concern | Library |
|---|---|
| Framework | .NET 10, C# 13 |
| Database | EF Core 9 + SQLite |
| Validation | FluentValidation |
| DI decoration | Scrutor |
| API explorer | Scalar |
| Testing | xUnit + FluentAssertions + Respawn |

## Technical Highlights

- **CQRS** with a custom lightweight dispatcher — no MediatR dependency
- **Clean Architecture** with strict dependency rules enforced between layers
- **Result-based error handling** — domain errors are returned, not thrown
- **Pessimistic per-account locking** via `SemaphoreSlim` to handle concurrent operations safely, with deadlock prevention on transfers
- **Append-only transaction log** as the audit trail, with account balance derived from current state
- **Auto-generated IBANs** following the RS MOD97 format
- **Paginated transaction history** with date range filtering

## Documentation

| Document | Description |
|---|---|
| [`docs/architecture.md`](docs/architecture.md) | Solution structure, layer rules, locking strategy, DI registration |
| [`docs/domain.md`](docs/domain.md) | Entities, value objects, and all business rules explicitly stated |
| [`docs/cqrs-pattern.md`](docs/cqrs-pattern.md) | CQRS interfaces, dispatcher wiring, and a full end-to-end worked example |
| [`docs/error-handling.md`](docs/error-handling.md) | `Result<T>`, `ValidationException`, HTTP status mapping, and error response shape |
| [`docs/api-conventions.md`](docs/api-conventions.md) | All endpoints, status codes, and request/response body shapes |
| [`docs/testing-conventions.md`](docs/testing-conventions.md) | Unit vs integration test setup, naming conventions, and SQLite configuration |
| [`docs/decisions.md`](docs/decisions.md) | Architecture Decision Records (ADRs) explaining key design choices |

## AI Usage

Claude was used as a brainstorming and design tool during the planning phase — talking through architectural decisions, tradeoffs, and potential edge cases before writing any code. Claude Code was then used to assist with the implementation based on the agreed design.

The architecture, patterns, and decisions in this project are ones I understand and can speak to — the AI tooling helped me move faster, not think less.
