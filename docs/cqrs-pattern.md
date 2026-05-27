# CQRS Pattern

## Interfaces

### Commands
```csharp
public interface ICommand<out TResult> { }

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct, bool saveChanges = true);
}

public interface ICommandDispatcher
{
    Task<TResult> DispatchAsync<TCommand, TResult>(
        TCommand command, CancellationToken ct, bool saveChanges = true)
        where TCommand : ICommand<TResult>;
}
```

### Queries
```csharp
public interface IQuery<out TResult> { }

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}

public interface IQueryDispatcher
{
    Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken ct)
        where TQuery : IQuery<TResult>;
}
```

## Dispatcher Implementations

### CommandDispatcher
```csharp
public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _provider;

    public CommandDispatcher(IServiceProvider provider) => _provider = provider;

    public Task<TResult> DispatchAsync<TCommand, TResult>(
        TCommand command, CancellationToken ct, bool saveChanges = true)
        where TCommand : ICommand<TResult>
    {
        var handler = _provider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        return handler.HandleAsync(command, ct, saveChanges);
    }
}
```

### QueryDispatcher (with inline validation)
```csharp
public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _provider;

    public QueryDispatcher(IServiceProvider provider) => _provider = provider;

    public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken ct)
        where TQuery : IQuery<TResult>
    {
        var validators = _provider.GetServices<IValidator<TQuery>>().ToList();

        if (validators.Count > 0)
        {
            var context = new ValidationContext<TQuery>(query);
            var errors = validators
                .Select(v => v.Validate(context))
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .Select(f => f.ErrorMessage)
                .ToList();

            if (errors.Count > 0)
                throw new BankingValidationException(errors);
        }

        var handler = _provider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return await handler.HandleAsync(query, ct);
    }
}
```

### ValidationCommandHandlerDecorator
Runs before every command handler. Throws `BankingValidationException` on failure.
```csharp
public class ValidationCommandHandlerDecorator<TCommand, TResult>
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _inner;
    private readonly IEnumerable<IValidator<TCommand>> _validators;

    public ValidationCommandHandlerDecorator(
        ICommandHandler<TCommand, TResult> inner,
        IEnumerable<IValidator<TCommand>> validators)
    {
        _inner = inner;
        _validators = validators;
    }

    public async Task<TResult> HandleAsync(TCommand command, CancellationToken ct, bool saveChanges = true)
    {
        var validators = _validators.ToList();

        if (validators.Count > 0)
        {
            var context = new ValidationContext<TCommand>(command);
            var errors = validators
                .Select(v => v.Validate(context))
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .Select(f => f.ErrorMessage)
                .ToList();

            if (errors.Count > 0)
                throw new BankingValidationException(errors);
        }

        return await _inner.HandleAsync(command, ct, saveChanges);
    }
}
```

---

## Worked Example — Command (CreateAccount)

### 1. Command
```csharp
// Application/Commands/CreateAccount/CreateAccountCommand.cs
public record CreateAccountCommand(
    string FirstName,
    string LastName,
    decimal InitialDeposit,
    Currency Currency
) : ICommand<Result<Guid>>;
```

### 2. Validator
```csharp
// Application/Commands/CreateAccount/CreateAccountCommandValidator.cs
public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.InitialDeposit)
            .GreaterThan(0).WithMessage("Initial deposit must be greater than zero.");

        RuleFor(x => x.Currency)
            .IsInEnum().WithMessage("Currency is not supported.");
    }
}
```

### 3. Handler
```csharp
// Application/Commands/CreateAccount/CreateAccountCommandHandler.cs
public class CreateAccountCommandHandler : ICommandHandler<CreateAccountCommand, Result<Guid>>
{
    private readonly BankingDbContext _context;
    private readonly IIbanGenerator _ibanGenerator;
    private readonly ICommandDispatcher _dispatcher;

    public CreateAccountCommandHandler(BankingDbContext context, IIbanGenerator ibanGenerator,
        ICommandDispatcher dispatcher)
    {
        _context = context;
        _ibanGenerator = ibanGenerator;
        _dispatcher = dispatcher;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateAccountCommand command, CancellationToken ct, bool saveChanges = true)
    {
        var now = DateTime.UtcNow;
        var account = CreateAccountMapper.ToEntity(command, _ibanGenerator.Generate(), now);
        _context.Accounts.Add(account);

        if (command.InitialDeposit > 0)
        {
            await _dispatcher.DispatchAsync<CreateTransactionCommand, Result<Guid>>(
                new CreateTransactionCommand(account.AccountId, TransactionType.Credit,
                    command.InitialDeposit, now, "Initial deposit"),
                ct, saveChanges: false);
        }

        if (saveChanges)
            await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(account.AccountId);
    }
}
```

### 4. IAccountService (facade — exposes to outside world)
```csharp
// Application/IAccountService.cs
public interface IAccountService
{
    Task<Result<Guid>> CreateAccountAsync(CreateAccountCommand command, CancellationToken ct);
    Task<Result<decimal>> DepositAsync(DepositCommand command, CancellationToken ct);
    Task<Result<decimal>> WithdrawAsync(WithdrawCommand command, CancellationToken ct);
    Task<Result> TransferAsync(TransferCommand command, CancellationToken ct);
    Task<Result<AccountDto>> GetAccountDetailsAsync(GetAccountDetailsQuery query, CancellationToken ct);
    Task<Result<AccountBalanceDto>> GetAccountBalanceAsync(GetAccountBalanceQuery query, CancellationToken ct);
    Task<Result<PagedResult<TransactionDto>>> GetAccountTransactionsAsync(GetAccountTransactionsQuery query, CancellationToken ct);
}
```

### 5. AccountService (facade implementation — locking lives here)
```csharp
// Application/AccountService.cs
public class AccountService : IAccountService
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly IAccountLockService _lockService;

    public AccountService(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher,
        IAccountLockService lockService)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _lockService = lockService;
    }

    public Task<Result<Guid>> CreateAccountAsync(CreateAccountCommand command, CancellationToken ct)
        // No lock needed — account doesn't exist yet
        => _commandDispatcher.DispatchAsync<CreateAccountCommand, Result<Guid>>(command, ct);

    public async Task<Result<decimal>> DepositAsync(DepositCommand command, CancellationToken ct)
    {
        await using var accountLock = await _lockService.AcquireAsync(command.AccountId, ct);
        return await _commandDispatcher.DispatchAsync<DepositCommand, Result<decimal>>(command, ct);
    }

    public async Task<Result<decimal>> WithdrawAsync(WithdrawCommand command, CancellationToken ct)
    {
        await using var accountLock = await _lockService.AcquireAsync(command.AccountId, ct);
        return await _commandDispatcher.DispatchAsync<WithdrawCommand, Result<decimal>>(command, ct);
    }

    public async Task<Result> TransferAsync(TransferCommand command, CancellationToken ct)
    {
        // Always lock in ascending ID order to prevent deadlock
        var (firstId, secondId) = command.FromAccountId < command.ToAccountId
            ? (command.FromAccountId, command.ToAccountId)
            : (command.ToAccountId, command.FromAccountId);

        await using var lock1 = await _lockService.AcquireAsync(firstId, ct);
        await using var lock2 = await _lockService.AcquireAsync(secondId, ct);

        return await _commandDispatcher.DispatchAsync<TransferCommand, Result>(command, ct);
    }

    public Task<Result<AccountDto>> GetAccountDetailsAsync(GetAccountDetailsQuery query, CancellationToken ct)
        => _queryDispatcher.DispatchAsync<GetAccountDetailsQuery, Result<AccountDto>>(query, ct);

    public Task<Result<AccountBalanceDto>> GetAccountBalanceAsync(GetAccountBalanceQuery query, CancellationToken ct)
        => _queryDispatcher.DispatchAsync<GetAccountBalanceQuery, Result<AccountBalanceDto>>(query, ct);

    public Task<Result<PagedResult<TransactionDto>>> GetAccountTransactionsAsync(GetAccountTransactionsQuery query, CancellationToken ct)
        => _queryDispatcher.DispatchAsync<GetAccountTransactionsQuery, Result<PagedResult<TransactionDto>>>(query, ct);
}
```

---

## Worked Example — Query (GetAccountDetails)

### 1. Query
```csharp
// Application/Queries/GetAccountDetails/GetAccountDetailsQuery.cs
public record GetAccountDetailsQuery(Guid AccountId) : IQuery<Result<AccountDto>>;
```

### 2. Validator (optional — only if structural validation is needed)
```csharp
public class GetAccountDetailsQueryValidator : AbstractValidator<GetAccountDetailsQuery>
{
    public GetAccountDetailsQueryValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("Account ID is required.");
    }
}
```

### 3. Handler
```csharp
// Application/Queries/GetAccountDetails/GetAccountDetailsQueryHandler.cs
public class GetAccountDetailsQueryHandler : IQueryHandler<GetAccountDetailsQuery, Result<AccountDto>>
{
    private readonly BankingDbContext _context;

    public GetAccountDetailsQueryHandler(BankingDbContext context) => _context = context;

    public async Task<Result<AccountDto>> HandleAsync(GetAccountDetailsQuery query, CancellationToken ct)
    {
        var dto = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.AccountId == query.AccountId)
            .Select(AccountDtoMapper.Projection())
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result<AccountDto>.Failure("Account not found.")
            : Result<AccountDto>.Success(dto);
    }
}
```

---

## Paginated Query Pattern

### Base record
```csharp
public record PagedQuery(int Page = 1, int PageSize = 20);
```

### Paginated query
```csharp
public record GetAccountTransactionsQuery(
    Guid AccountId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20
) : PagedQuery(Page, PageSize), IQuery<Result<PagedResult<TransactionDto>>>;
```

### Handler — applying pagination with EF Core
```csharp
var query = _context.Transactions
    .AsNoTracking()
    .Where(t => t.AccountId == request.AccountId);

if (request.FromDate.HasValue)
    query = query.Where(t => t.CreatedAt >= request.FromDate.Value);

if (request.ToDate.HasValue)
    query = query.Where(t => t.CreatedAt <= request.ToDate.Value);

var totalCount = await query.CountAsync(ct);

var items = await query
    .OrderByDescending(t => t.CreatedAt)
    .Skip((request.Page - 1) * request.PageSize)
    .Take(request.PageSize)
    .Select(TransactionDtoMapper.Projection())
    .ToListAsync(ct);

return Result<PagedResult<TransactionDto>>.Success(
    new PagedResult<TransactionDto>(items, request.Page, request.PageSize, totalCount));
```

---

## Transfer — saveChanges: false pattern

The Transfer handler calls Deposit and Withdrawal sub-handlers with `saveChanges: false`,
then saves everything in a single EF Core transaction.

```csharp
public async Task<Result> HandleAsync(TransferCommand command, CancellationToken ct, bool saveChanges = true)
{
    await using var transaction = await _context.Database.BeginTransactionAsync(ct);

    try
    {
        var debitResult = await _debitHandler.HandleAsync(
            new DebitCommand(command.FromAccountId, command.Amount, command.ToAccountId, command.Description),
            ct, saveChanges: false);

        if (debitResult.IsFailure)
            return Result.Failure(debitResult.Errors);

        var creditResult = await _creditHandler.HandleAsync(
            new CreditCommand(command.ToAccountId, command.Amount, command.FromAccountId, command.Description),
            ct, saveChanges: false);

        if (creditResult.IsFailure)
            return Result.Failure(creditResult.Errors);

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Result.Success();
    }
    catch
    {
        await transaction.RollbackAsync(ct);
        throw;
    }
}
```