using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Domain.Entities;
using BankingService.Domain.Enums;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Application.Commands.Deposit;

public class DepositCommandHandler : ICommandHandler<DepositCommand, Result<MoneyDto>>
{
    private readonly BankingDbContext _context;

    public DepositCommandHandler(BankingDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MoneyDto>> HandleAsync(DepositCommand command, CancellationToken ct, bool saveChanges = true)
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

        _context.Transactions.Add(new Transaction
        {
            TransactionId = Guid.NewGuid(),
            AccountId = account.AccountId,
            Type = TransactionType.Credit,
            Amount = command.Amount,
            Description = "Deposit",
            CreatedAt = now
        });

        if (saveChanges)
        {
            await _context.SaveChangesAsync(ct);
        }

        return Result<MoneyDto>.Success(new MoneyDto(account.Balance.Amount, account.Balance.Currency));
    }
}
