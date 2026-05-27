using BankingService.Application.Commands.CreateTransaction;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Domain.Enums;
using BankingService.Domain.Exceptions;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankingService.Application.Commands.Deposit;

public class DepositCommandHandler : ICommandHandler<DepositCommand, Result<MoneyDto>>
{
    private readonly BankingDbContext _context;
    private readonly ICommandDispatcher _dispatcher;
    private readonly ILogger<DepositCommandHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public DepositCommandHandler(BankingDbContext context, ICommandDispatcher dispatcher,
        ILogger<DepositCommandHandler> logger, TimeProvider timeProvider)
    {
        _context = context;
        _dispatcher = dispatcher;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MoneyDto>> HandleAsync(DepositCommand command, CancellationToken ct,
        bool saveChanges = true)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == command.AccountId, ct);

        if (account is null)
        {
            _logger.LogWarning("Deposit failed. AccountId: {AccountId}, Reason: {Reason}",
                command.AccountId, "Account not found");
            return Result<MoneyDto>.Failure("Account not found.");
        }

        if (!account.IsActive)
        {
            _logger.LogWarning("Deposit failed. AccountId: {AccountId}, Reason: {Reason}",
                command.AccountId, "Account is not active");
            return Result<MoneyDto>.Failure("Account is not active.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            account.Deposit(command.Amount, now);
        }
        catch (CurrencyMismatchException)
        {
            _logger.LogWarning("Deposit failed. AccountId: {AccountId}, Reason: {Reason}",
                command.AccountId, "Currency mismatch");
            return Result<MoneyDto>.Failure("Deposit currency does not match account currency.");
        }

        await _dispatcher.DispatchAsync<CreateTransactionCommand, Result<Guid>>(
            new CreateTransactionCommand(account.AccountId, TransactionType.Credit, command.Amount, now,
                command.Description ?? "Deposit", command.RelatedAccountId),
            ct, saveChanges: false);

        if (saveChanges)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Deposit succeeded. AccountId: {AccountId}, Amount: {Amount} {Currency}, NewBalance: {NewBalance}",
            account.AccountId, command.Amount.Amount, command.Amount.Currency, account.Balance.Amount);
        return Result<MoneyDto>.Success(new MoneyDto(account.Balance.Amount, account.Balance.Currency));
    }
}