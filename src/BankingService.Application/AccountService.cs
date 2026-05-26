using BankingService.Application.Commands.CreateAccount;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
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
}