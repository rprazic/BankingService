# Domain Model

## Entities

### Account
```
AccountId    : Guid          — primary key, generated on creation
FirstName    : string        — required, max 100 chars
LastName     : string        — required, max 100 chars
Iban         : string        — auto-generated on creation, unique, immutable
Currency     : Currency      — set on creation, immutable
Balance      : Money         — current balance, updated on every transaction
IsActive     : bool          — true by default; set to false on freeze/close
CreatedAt    : DateTime      — UTC, set on creation, immutable
UpdatedAt    : DateTime      — UTC, updated on every mutating operation
```

`Balance.Currency` always equals `Account.Currency`. This invariant is enforced at creation and on every operation.

### Transaction
Append-only event log. **Never updated, never deleted.**
```
TransactionId     : Guid              — primary key, generated on creation
AccountId         : Guid              — FK to Account
Type              : TransactionType   — Credit | Debit
Amount            : Money             — always positive
RelatedAccountId  : Guid?             — only set for transfers (the other account)
Description       : string?           — optional, max 500 chars
CreatedAt         : DateTime          — UTC, set on creation
```

`Account.Balance` is the source of truth for current balance.
`Transaction` records are the immutable audit trail.

## Value Objects

### Money
```
Amount    : decimal    — precision 18, scale 4
Currency  : Currency
```

Key behaviors:
- Full arithmetic operator overloading (`+`, `-`, `*`, `/`)
- Cross-currency operations (`Money + Money`, `Money - Money`, etc.) throw `CurrencyMismatchException`
- Division: only `Money / decimal` is supported (dividing money by scalar); `decimal / Money` is not defined
- Uses `MidpointRounding.ToEven` (banker's rounding) for multiplication
- Parameterless constructor is private — for EF Core materialization only
- Negative amounts are **not** rejected by the VO; validation is the responsibility of command validators

### BaseValueObject
Provides structural equality via `GetEqualityComponents()`. All VOs inherit from it.

## Enums

### Currency
```csharp
public enum Currency { EUR, USD, RSD }
```

### TransactionType
```csharp
public enum TransactionType { Credit, Debit }
```

`Credit` = money arriving on the account (deposit, transfer in).
`Debit` = money leaving the account (withdrawal, transfer out).
`Transaction.RelatedAccountId` being set is what distinguishes a transfer from a regular deposit or withdrawal — not the type.

## Business Rules

### Account creation
- `FirstName` and `LastName` are required, max 100 characters each
- `InitialDeposit` must be greater than zero
- `Currency` must be a valid `Currency` enum value
- IBAN is auto-generated — callers cannot provide their own

### Deposit
- `Amount` must be greater than zero
- `Amount.Currency` must match `Account.Currency`
- Balance is updated: `Account.Balance += Amount`
- A `Transaction` record of type `Credit` is appended (`RelatedAccountId` is null)

### Withdrawal
- `Amount` must be greater than zero
- `Amount.Currency` must match `Account.Currency`
- `Account.Balance` must be greater than or equal to `Amount` — **overdrafts are not allowed**
- Balance is updated: `Account.Balance -= Amount`
- A `Transaction` record of type `Debit` is appended (`RelatedAccountId` is null)

### Transfer
- `FromAccountId` must not equal `ToAccountId`
- Both accounts must exist
- Both accounts must have the same `Currency` — cross-currency transfers are not supported
- `Amount` must be greater than zero
- `FromAccount.Balance` must be greater than or equal to `Amount`
- `FromAccount.Balance -= Amount`, `ToAccount.Balance += Amount`
- Two `Transaction` records appended:
  - `Debit` on `FromAccount` with `RelatedAccountId = ToAccountId`
  - `Credit` on `ToAccount` with `RelatedAccountId = FromAccountId`
- The entire operation is atomic — either both records and both balance updates succeed, or nothing changes

### Transaction query filtering
- `FromDate` and `ToDate` filter on `Transaction.CreatedAt` (inclusive on both ends)
- Both are optional; omitting them returns all transactions for the account
- Results are ordered by `CreatedAt` descending

## IBAN Generation (Serbia — RS)

Format: `RS` + 2 check digits + 18-digit account number

Algorithm (MOD97):
1. Generate a random 18-digit numeric string as the BBAN
2. Form the string: `BBAN + "RS" + "00"`
3. Replace letters with numbers: `R=27`, `S=28`
4. Compute: `checkDigits = 98 - (numericString MOD 97)`
5. Zero-pad `checkDigits` to 2 digits
6. Return `"RS" + checkDigits + BBAN`

`IIbanGenerator` interface lives in `BankingService.Infrastructure.Services`. `IbanGenerator` implementation is in the same namespace. Both are accessible from Application (Application → Infrastructure).