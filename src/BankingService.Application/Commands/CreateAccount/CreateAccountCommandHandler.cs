using BankingService.Application.Commands.CreateTransaction;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.Mappings;
using BankingService.Domain.Enums;
using BankingService.Infrastructure.Persistence;
using BankingService.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace BankingService.Application.Commands.CreateAccount;

public class CreateAccountCommandHandler : ICommandHandler<CreateAccountCommand, Result<Guid>>
{
    private readonly BankingDbContext _context;
    private readonly IIbanGenerator _ibanGenerator;
    private readonly ICommandDispatcher _dispatcher;
    private readonly ILogger<CreateAccountCommandHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public CreateAccountCommandHandler(BankingDbContext context, IIbanGenerator ibanGenerator,
        ICommandDispatcher dispatcher, ILogger<CreateAccountCommandHandler> logger, TimeProvider timeProvider)
    {
        _context = context;
        _ibanGenerator = ibanGenerator;
        _dispatcher = dispatcher;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(CreateAccountCommand command, CancellationToken ct,
        bool saveChanges = true)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var account = CreateAccountMapper.ToEntity(command, _ibanGenerator.Generate(), now);
        _context.Accounts.Add(account);

        if (command.InitialDeposit > 0)
        {
            await _dispatcher.DispatchAsync<CreateTransactionCommand, Result<Guid>>(
                new CreateTransactionCommand(account.AccountId, TransactionType.Credit, command.InitialDeposit, now,
                    "Initial deposit"),
                ct, saveChanges: false);
        }

        if (saveChanges)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Account created. AccountId: {AccountId}", account.AccountId);
        return Result<Guid>.Success(account.AccountId);
    }
}