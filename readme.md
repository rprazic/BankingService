# BankingService

A .NET 9 banking service implementing core account operations: account creation, deposits, withdrawals, and transfers between accounts.

## Getting Started

### Prerequisites
- .NET 9 SDK
- No additional infrastructure required — SQLite is used as the database

### Run the API
```bash
dotnet run --project src/BankingService.Api
```

Scalar is available at `http://localhost:5000/scalar/v1`.

### Run Tests
```bash
# Unit tests
dotnet test tests/BankingService.Unit.Tests

# Integration tests
dotnet test tests/BankingService.Integration.Tests
```

### Docker
```bash
# Run from solution root
docker build -f src/BankingService.Api/Dockerfile -t banking-service .
docker run -p 8080:8080 -v banking_data:/app/data banking-service
```

## Project Structure

```
src/
├── BankingService.Domain/         # Entities, value objects, enums
├── BankingService.Application/    # CQRS, commands, queries, service facade
├── BankingService.Infrastructure/ # EF Core, SQLite, locking, IBAN generation
└── BankingService.Api/           # Minimal API endpoints, Scalar
tests/
├── BankingService.Unit.Tests/
└── BankingService.Integration.Tests/
```

## Technical Highlights

- **CQRS** with a custom lightweight implementation — no MediatR dependency
- **Clean Architecture** with strict dependency rules between layers
- **Result-based error handling** — domain errors are returned, not thrown
- **Pessimistic per-account locking** via `SemaphoreSlim` to handle concurrent operations safely, with deadlock prevention on transfers
- **Append-only transaction log** as the audit trail, with account balance as the current state
- **Auto-generated IBANs** following the RS MOD97 format
- **Paginated transaction history** with date range filtering

## AI Usage

Claude was used as a brainstorming and design tool during the planning phase — talking through architectural decisions, tradeoffs, and potential edge cases before writing any code. Claude Code was then used to assist with the implementation based on the agreed design.

The architecture, patterns, and decisions in this project are ones I understand and can speak to — the AI tooling helped me move faster, not think less.