using BankingService.Application.Commands.CreateTransaction;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Domain.Enums;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Application.Commands.Withdraw;

public class WithdrawCommandHandler : ICommandHandler<WithdrawCommand, Result<MoneyDto>>
{
    private readonly BankingDbContext _context;
    private readonly ICommandDispatcher _dispatcher;

    public WithdrawCommandHandler(BankingDbContext context, ICommandDispatcher dispatcher)
    {
        _context = context;
        _dispatcher = dispatcher;
    }

    public async Task<Result<MoneyDto>> HandleAsync(WithdrawCommand command, CancellationToken ct, bool saveChanges = true)
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
            return Result<MoneyDto>.Failure("Withdrawal currency does not match account currency.");
        }

        if (command.Amount.Amount > account.Balance.Amount)
        {
            return Result<MoneyDto>.Failure("Insufficient funds.");
        }

        var now = DateTime.UtcNow;
        account.Balance -= command.Amount;
        account.UpdatedAt = now;

        await _dispatcher.DispatchAsync<CreateTransactionCommand, Result<Guid>>(
            new CreateTransactionCommand(account.AccountId, TransactionType.Debit, command.Amount, now, "Withdrawal"),
            ct, saveChanges: false);

        if (saveChanges)
        {
            await _context.SaveChangesAsync(ct);
        }

        return Result<MoneyDto>.Success(new MoneyDto(account.Balance.Amount, account.Balance.Currency));
    }
}
