using BankingService.Application.Commands.CreateTransaction;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Domain.Enums;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankingService.Application.Commands.Withdraw;

public class WithdrawCommandHandler : ICommandHandler<WithdrawCommand, Result<MoneyDto>>
{
    private readonly BankingDbContext _context;
    private readonly ICommandDispatcher _dispatcher;
    private readonly ILogger<WithdrawCommandHandler> _logger;

    public WithdrawCommandHandler(BankingDbContext context, ICommandDispatcher dispatcher,
        ILogger<WithdrawCommandHandler> logger)
    {
        _context = context;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task<Result<MoneyDto>> HandleAsync(WithdrawCommand command, CancellationToken ct,
        bool saveChanges = true)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == command.AccountId, ct);

        if (account is null)
        {
            _logger.LogWarning("Withdrawal failed. AccountId: {AccountId}, Reason: {Reason}",
                command.AccountId, "Account not found");
            return Result<MoneyDto>.Failure("Account not found.");
        }

        if (!account.IsActive)
        {
            _logger.LogWarning("Withdrawal failed. AccountId: {AccountId}, Reason: {Reason}",
                command.AccountId, "Account is not active");
            return Result<MoneyDto>.Failure("Account is not active.");
        }

        if (command.Amount.Currency != account.Currency)
        {
            _logger.LogWarning("Withdrawal failed. AccountId: {AccountId}, Reason: {Reason}",
                command.AccountId, "Currency mismatch");
            return Result<MoneyDto>.Failure("Withdrawal currency does not match account currency.");
        }

        if (command.Amount.Amount > account.Balance.Amount)
        {
            _logger.LogWarning("Withdrawal failed. AccountId: {AccountId}, Reason: {Reason}",
                command.AccountId, "Insufficient funds");
            return Result<MoneyDto>.Failure("Insufficient funds.");
        }

        var now = DateTime.UtcNow;
        account.Balance -= command.Amount;
        account.UpdatedAt = now;

        await _dispatcher.DispatchAsync<CreateTransactionCommand, Result<Guid>>(
            new CreateTransactionCommand(account.AccountId, TransactionType.Debit, command.Amount, now,
                command.Description ?? "Withdrawal"),
            ct, saveChanges: false);

        if (saveChanges)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Withdrawal succeeded. AccountId: {AccountId}, Amount: {Amount} {Currency}, NewBalance: {NewBalance}",
            account.AccountId, command.Amount.Amount, command.Amount.Currency, account.Balance.Amount);
        return Result<MoneyDto>.Success(new MoneyDto(account.Balance.Amount, account.Balance.Currency));
    }
}