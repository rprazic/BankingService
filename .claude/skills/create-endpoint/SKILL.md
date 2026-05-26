---
name: create-endpoint
description: Adds a new minimal API endpoint to AccountEndpoints.cs in the BankingService API. Use when exposing a command or query through the HTTP layer — generates route registration, handler method, and request DTO.
---

# Create Endpoint

Adds to `src/BankingService.Api/Endpoints/AccountEndpoints.cs`:
- Route registration inside `MapAccountEndpoints`
- Private static async handler method
- Request DTO (if needed)

## Before generating, ask

- Which `IAccountService` method does this endpoint call?
- What is the request body shape?
- What does a successful response look like?
- 200 OK, 201 Created, or no body?

## Templates

### Command endpoint
```csharp
private static async Task<IResult> {MethodName}(
    [FromRoute] Guid accountId,
    [FromBody] {RequestDto} request,
    IAccountService accountService,
    CancellationToken ct)
{
    var command = new {CommandName}(accountId, request.Amount, request.Description);
    var result = await accountService.{ServiceMethod}(command, ct);

    return result.IsSuccess
        ? Results.Ok(new { /* response */ })
        : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
}
```

### Creation endpoint (201)
```csharp
private static async Task<IResult> CreateAccount(
    [FromBody] CreateAccountRequest request,
    IAccountService accountService,
    CancellationToken ct)
{
    var command = new CreateAccountCommand(
        request.FirstName, request.LastName, request.InitialDeposit, request.Currency);
    var result = await accountService.CreateAccountAsync(command, ct);

    return result.IsSuccess
        ? Results.Created($"/api/v1/accounts/{result.Value}", new { AccountId = result.Value })
        : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
}
```

### Query endpoint
```csharp
private static async Task<IResult> {MethodName}(
    [FromRoute] Guid accountId,
    IAccountService accountService,
    CancellationToken ct)
{
    var query = new {QueryName}(accountId);
    var result = await accountService.{ServiceMethod}(query, ct);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
}
```

### Request DTO
```csharp
private sealed record {RequestDto}(decimal Amount, string? Description);
```

### Route registration
```csharp
group.MapPost("/{accountId:guid}/deposits", Deposit)
    .WithSummary("Deposit money")
    .WithDescription("Deposits the specified amount. Returns the new balance.");
```

## Status code rules

| Scenario | Status |
|---|---|
| Command success with value | `Results.Ok(value)` |
| Command success, no value | `Results.Ok()` |
| Resource created | `Results.Created(location, body)` |
| Domain failure | `Results.UnprocessableEntity(new ErrorResponse(errors))` |

Never return 404 for domain failures — use 422 with a descriptive message.
Validation failures (400) are handled by `ExceptionMiddleware` automatically.

## Checklist

- [ ] Route registered in `MapAccountEndpoints` with `WithSummary`
- [ ] Handler is `private static async Task<IResult>`
- [ ] Request DTO defined if needed (separate from command record)
- [ ] Correct success status (200 vs 201)
- [ ] Domain failures return 422 with `ErrorResponse`