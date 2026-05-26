# BankingService — Claude Code Instructions

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