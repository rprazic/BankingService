using BankingService.Application.Commands.CreateAccount;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Application.Queries.GetAccountBalance;
using BankingService.Application.Queries.GetAccountDetails;
using BankingService.Application.Queries.GetAccountTransactions;
using BankingService.Infrastructure.Locking;

namespace BankingService.Application;

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
        => _commandDispatcher.DispatchAsync<CreateAccountCommand, Result<Guid>>(command, ct);

    public Task<Result<AccountDto>> GetAccountDetailsAsync(GetAccountDetailsQuery query, CancellationToken ct)
        => _queryDispatcher.DispatchAsync<GetAccountDetailsQuery, Result<AccountDto>>(query, ct);

    public Task<Result<AccountBalanceDto>> GetAccountBalanceAsync(GetAccountBalanceQuery query, CancellationToken ct)
        => _queryDispatcher.DispatchAsync<GetAccountBalanceQuery, Result<AccountBalanceDto>>(query, ct);

    public Task<Result<PagedResult<TransactionDto>>> GetAccountTransactionsAsync(GetAccountTransactionsQuery query,
        CancellationToken ct)
        => _queryDispatcher.DispatchAsync<GetAccountTransactionsQuery, Result<PagedResult<TransactionDto>>>(query, ct);
}