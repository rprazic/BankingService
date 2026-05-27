using BankingService.Application.Commands.CreateTransaction;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Domain.Enums;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Application.Commands.Deposit;

public class DepositCommandHandler : ICommandHandler<DepositCommand, Result<MoneyDto>>
{
    private readonly BankingDbContext _context;
    private readonly ICommandDispatcher _dispatcher;

    public DepositCommandHandler(BankingDbContext context, ICommandDispatcher dispatcher)
    {
        _context = context;
        _dispatcher = dispatcher;
    }

    public async Task<Result<MoneyDto>> HandleAsync(DepositCommand command, CancellationToken ct,
        bool saveChanges = true)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == command.AccountId, ct);

        if (account is null)
        {
            return Result<MoneyDto>.Failure("Account not found.");
        }

        if (!account.IsActive)
        {
            return Result<MoneyDto>.Failure("Account is not active.");
        }

        if (command.Amount.Currency != account.Currency)
        {
            return Result<MoneyDto>.Failure("Deposit currency does not match account currency.");
        }

        var now = DateTime.UtcNow;
        account.Balance += command.Amount;
        account.UpdatedAt = now;

        await _dispatcher.DispatchAsync<CreateTransactionCommand, Result<Guid>>(
            new CreateTransactionCommand(account.AccountId, TransactionType.Credit, command.Amount, now,
                command.Description ?? "Deposit"),
            ct, saveChanges: false);

        if (saveChanges)
        {
            await _context.SaveChangesAsync(ct);
        }

        return Result<MoneyDto>.Success(new MoneyDto(account.Balance.Amount, account.Balance.Currency));
    }
}