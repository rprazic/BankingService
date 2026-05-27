# API Conventions

## Base URL
All endpoints are under `/api/v1` prefix.
Scalar is served at `/scalar/v1` (default page).

## Endpoints

### POST /api/v1/accounts
Create a new account.

**Request body:**
```json
{
  "firstName": "Ratko",
  "lastName": "Petrović",
  "initialDeposit": 1000.00,
  "currency": "EUR"
}
```

**Response 201 Created:**
```json
{
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```
`Location` header: `/api/v1/accounts/{accountId}`

---

### GET /api/v1/accounts/{accountId}
Get full account details including current balance.

**Response 200 OK:**
```json
{
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "firstName": "Ratko",
  "lastName": "Petrović",
  "iban": "RS35105008123123123173",
  "currency": "EUR",
  "balance": 1000.00,
  "createdAt": "2025-01-15T10:30:00Z"
}
```

---

### GET /api/v1/accounts/{accountId}/balance
Get current balance only (lightweight).

**Response 200 OK:**
```json
{
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "balance": 1000.00,
  "currency": "EUR"
}
```

---

### POST /api/v1/accounts/{accountId}/deposits
Deposit money into an account.

**Request body:**
```json
{
  "amount": 500.00,
  "description": "Monthly salary"
}
```

**Response 200 OK:**
```json
{
  "newBalance": 1500.00
}
```

---

### POST /api/v1/accounts/{accountId}/withdrawals
Withdraw money from an account.

**Request body:**
```json
{
  "amount": 200.00,
  "description": "Rent payment"
}
```

**Response 200 OK:**
```json
{
  "newBalance": 1300.00
}
```

---

### POST /api/v1/accounts/{accountId}/transfers
Transfer money to another account.

**Request body:**
```json
{
  "toAccountId": "7cb45f64-1234-4562-b3fc-2c963f66afa6",
  "amount": 300.00,
  "description": "Paying my rent"
}
```

**Response 200 OK** (empty body)

---

### GET /api/v1/accounts/{accountId}/transactions
Get paginated transaction history with optional date filters.

**Query parameters:**
| Param | Type | Required | Description |
|---|---|---|---|
| `page` | int | No | Default: 1 |
| `pageSize` | int | No | Default: 20, max: 100 |
| `fromDate` | ISO 8601 | No | Inclusive lower bound |
| `toDate` | ISO 8601 | No | Inclusive upper bound |

**Example:** `/api/v1/accounts/{id}/transactions?page=1&pageSize=10&fromDate=2025-01-01`

**Response 200 OK:**
```json
{
  "items": [
    {
      "transactionId": "...",
      "type": "Credit",
      "amount": 500.00,
      "currency": "EUR",
      "relatedAccountId": null,
      "description": "Monthly salary",
      "createdAt": "2025-01-20T14:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

---

## Status Code Summary

| Status | When |
|---|---|
| 200 OK | Successful read or update |
| 201 Created | Account created |
| 400 Bad Request | Validation failure (invalid input) |
| 422 Unprocessable Entity | Domain failure (insufficient funds, account not found, etc.) |
| 429 Too Many Requests | Rate limit exceeded on a mutation endpoint |
| 500 Internal Server Error | Unexpected server error |

## Error Response (400, 422, and 429)
```json
{
  "errors": ["Insufficient funds.", "Amount must be greater than zero."]
}
```

## Rate Limiting

Mutation endpoints (`POST /accounts`, `POST /deposits`, `POST /withdrawals`, `POST /transfers`) are rate-limited using a fixed window policy:

- **Limit**: 10 requests per minute per IP address
- **Exceeded**: `429 Too Many Requests` with an `ErrorResponse` body
- Read-only endpoints (`GET`) are not rate-limited

## Endpoint Structure

Each endpoint lives in its own file under `BankingService.Api/Endpoints/` with a single `MapXxx` extension method on `IEndpointRouteBuilder`.

```
Endpoints/
├── CreateAccountEndpoint.cs       — MapCreateAccount()
├── GetAccountDetailsEndpoint.cs   — MapGetAccountDetails()
├── GetAccountBalanceEndpoint.cs   — MapGetAccountBalance()
├── DepositEndpoint.cs             — MapDeposit()
├── WithdrawEndpoint.cs            — MapWithdraw()
├── TransferEndpoint.cs            — MapTransfer()
└── GetAccountTransactionsEndpoint.cs — MapGetAccountTransactions()
```

Request/response models live in `BankingService.Api/Models/` under the `BankingService.Api.Models` namespace:

```
Models/
├── ErrorResponse.cs
├── CreateAccountRequest.cs
├── CreateAccountResponse.cs
├── DepositRequest.cs
├── WithdrawRequest.cs
└── TransferRequest.cs
```

Called from `Program.cs`:
```csharp
app.MapCreateAccount();
app.MapGetAccountDetails();
app.MapGetAccountBalance();
app.MapDeposit();
app.MapWithdraw();
app.MapTransfer();
app.MapGetAccountTransactions();
```
