# Error Handling

## Result Types

### Result (non-generic) — for operations with no return value (e.g. Transfer)
```csharp
public class Result
{
    protected Result(bool isSuccess, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }

    public static Result Success() => new(true, []);
    public static Result Failure(string error) => new(false, [error]);
    public static Result Failure(IReadOnlyList<string> errors) => new(false, errors);
}
```

### Result<T> — for operations that return a value on success
```csharp
public class Result<T> : Result
{
    private Result(T value) : base(true, []) => Value = value;
    private Result(IReadOnlyList<string> errors) : base(false, errors) => Value = default;

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(string error) => new([error]);
    public new static Result<T> Failure(IReadOnlyList<string> errors) => new(errors);
}
```

### PagedResult<T> — for paginated query results
```csharp
public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

## BankingValidationException

Thrown by `ValidationCommandHandlerDecorator` and `QueryDispatcher` when FluentValidation fails.
Never thrown from handlers.

```csharp
public class BankingValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public BankingValidationException(IReadOnlyList<string> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
```

## Two-Track Error Model

| Error type | Source | How surfaced |
|---|---|---|
| Validation errors | FluentValidation (input shape, bounds) | `BankingValidationException` → 400 |
| Domain errors | Business rule violations in handlers | `Result.Failure(...)` → 422 |
| Infrastructure errors | Unexpected exceptions (DB down, etc.) | Unhandled exception → 500 |

**Key distinction:**
- "Amount must be greater than zero" → **validation error** (validator catches it before handler runs)
- "Insufficient funds" → **domain error** (handler returns `Result.Failure(...)`)
- "Account not found" → **domain error** (handler returns `Result.Failure(...)`)

## Global Exception Middleware

```csharp
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BankingValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ErrorResponse(ex.Errors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                new ErrorResponse(["An unexpected error occurred."]));
        }
    }
}
```

## Error Response Shape

All error responses (400 and 500) use the same JSON shape:

```json
{
  "errors": [
    "First name is required.",
    "Amount must be greater than zero."
  ]
}
```

```csharp
public record ErrorResponse(IReadOnlyList<string> Errors);
```

## HTTP Status Code Mapping

| Scenario | Status code |
|---|---|
| Validation failure (invalid input) | 400 Bad Request |
| Domain failure (insufficient funds, not found, etc.) | 422 Unprocessable Entity |
| Success with body | 200 OK |
| Success with created resource | 201 Created |
| Unexpected server error | 500 Internal Server Error |

## Mapping Result in Endpoints

```csharp
// Pattern for commands that return a value
app.MapPost("/accounts/{id}/deposits", async (...) =>
{
    var result = await accountService.DepositAsync(command, ct);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
});

// Pattern for commands with no return value (Transfer)
app.MapPost("/accounts/{id}/transfers", async (...) =>
{
    var result = await accountService.TransferAsync(command, ct);

    return result.IsSuccess
        ? Results.Ok()
        : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
});

// Pattern for creation
app.MapPost("/accounts", async (...) =>
{
    var result = await accountService.CreateAccountAsync(command, ct);

    return result.IsSuccess
        ? Results.Created($"/accounts/{result.Value}", new { AccountId = result.Value })
        : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
});
```