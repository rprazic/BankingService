using BankingService.Application.Commands.CreateTransaction;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.Mappings;
using BankingService.Domain.Enums;
using BankingService.Infrastructure.Persistence;
using BankingService.Infrastructure.Services;

namespace BankingService.Application.Commands.CreateAccount;

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

    public async Task<Result<Guid>> HandleAsync(CreateAccountCommand command, CancellationToken ct,
        bool saveChanges = true)
    {
        var now = DateTime.UtcNow;
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

        return Result<Guid>.Success(account.AccountId);
    }
}
