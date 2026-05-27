# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# BankingService — Claude Code Instructions

## Common Commands

```bash
# Run the API
dotnet run --project src/BankingService.Api

# Build
dotnet build

# All tests
dotnet test

# Unit tests only
dotnet test tests/BankingService.Unit.Tests

# Integration tests only
dotnet test tests/BankingService.Integration.Tests

# Single test by name filter
dotnet test --filter "FullyQualifiedName~HandleAsync_WithValidCommand"

# Docker
docker build -f src/BankingService.Api/Dockerfile -t banking-service .
docker run -p 8080:8080 -v banking_data:/app/data banking-service
```

## Project Overview
A .NET 10 banking service implementing account management, deposits, withdrawals, and transfers.
Built with Clean Architecture, CQRS, EF Core + SQLite, and a strict Result-based error handling strategy.

## Solution Structure
```
BankingService.sln
├── src/
│   ├── BankingService.Domain/            # Entities, VOs, enums — zero dependencies
│   ├── BankingService.Application/       # CQRS infrastructure, commands, queries, facade, DTOs
│   ├── BankingService.Infrastructure/    # EF Core, SQLite, locking, IBAN generation
│   └── BankingService.Api/              # Minimal API endpoints, Swagger, middleware
└── tests/
    ├── BankingService.Unit.Tests/        # Handler-level tests, SQLite in-memory
    └── BankingService.Integration.Tests/ # IAccountService-level tests, real SQLite
```

## Non-Negotiable Rules

### Architecture
1. **Never create repositories.** Use `BankingDbContext` directly in handlers.
2. **Never reference Infrastructure from Application or Domain.** Dependency direction: `Api → Application ← Infrastructure`, both pointing inward to `Domain`.
3. **Never add new NuGet packages** without checking `docs/architecture.md` for the approved tech stack.

### CQRS
4. **Commands are records.** Immutable, no public parameterless constructors.
5. **Always return `Result<T>` or `Result` from command handlers.** Never throw domain exceptions from handlers — return `Result.Failure(...)` instead.
6. **Validation lives in validators, not handlers.** Handlers assume input is already valid.
7. **Always use the dispatcher.** Never resolve and call handlers directly.

### Locking
8. **Never add locking inside handlers.** All locking is managed exclusively by `AccountService` (the facade). See `docs/architecture.md`.

### Testing
9. **Every handler must have a unit test.** No exceptions.
10. **Unit tests use SQLite in-memory.** Integration tests use a real `.db` file.
11. **Test naming:** `MethodName_StateUnderTest_ExpectedBehavior` — e.g. `HandleAsync_WithInsufficientFunds_ReturnsFailure`.
12. **Keep `SqliteConnection` open** for the test class lifetime in unit tests — closing it drops the in-memory schema.

### Error Handling (two-track model)
13. **Validation errors** (input shape, bounds) → `BankingValidationException` → 400. Raised by the decorator, never by handlers.
14. **Domain errors** (insufficient funds, not found) → `Result.Failure(...)` → 422. Returned from handlers, never thrown.
15. **Mutation endpoints are rate-limited** (10 req/min/IP). Exceeded requests return 429 with an `ErrorResponse` body.

### Multi-step commands (saveChanges: false)
16. **When a handler calls sub-handlers**, pass `saveChanges: false` to each and call `SaveChangesAsync` once at the end (see `TransferCommandHandler`). Wrap in an EF Core `BeginTransactionAsync` for atomicity.

## How to Add Features

Skills live in `.claude/skills/` and trigger automatically based on context.

| Task | Skill |
|---|---|
| New command (e.g. `FreezeAccount`) | `create-command` |
| New query (e.g. `GetAccountStatement`) | `create-query` |
| New API endpoint | `create-endpoint` |
| New EF Core migration | `create-migration` |

API documentation available at `/scalar/v1` when running.

## Key Documentation

| Doc | Purpose |
|---|---|
| `docs/architecture.md` | Solution structure, layer rules, locking, DI registration |
| `docs/domain.md` | Entities, VOs, all business rules explicitly stated |
| `docs/cqrs-pattern.md` | CQRS interfaces + full worked example, end to end |
| `docs/error-handling.md` | Result<T>, ValidationException, HTTP mapping, error shape |
| `docs/api-conventions.md` | Endpoints, status codes, request/response bodies |
| `docs/testing-conventions.md` | Unit vs integration, setup, naming conventions |
| `docs/decisions.md` | Why we made key architectural decisions (ADRs) |

## Tech Stack
- .NET 10, C# 13
- EF Core 9 + Microsoft.EntityFrameworkCore.Sqlite
- FluentValidation
- Scrutor (assembly scanning + open-generic decoration)
- Scalar
- xUnit + FluentAssertions + Respawn (tests)