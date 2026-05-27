using BankingService.Application.Commands.Deposit;
using BankingService.Application.Commands.Withdraw;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Domain.Entities;
using BankingService.Domain.ValueObjects;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankingService.Application.Commands.Transfer;

public class TransferCommandHandler : ICommandHandler<TransferCommand, Result>
{
    private readonly BankingDbContext _context;
    private readonly ICommandDispatcher _dispatcher;
    private readonly ILogger<TransferCommandHandler> _logger;

    public TransferCommandHandler(BankingDbContext context, ICommandDispatcher dispatcher,
        ILogger<TransferCommandHandler> logger)
    {
        _context = context;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(TransferCommand command, CancellationToken ct, bool saveChanges = true)
    {
        var sourceAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == command.FromAccountId, ct);
        var destinationAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == command.ToAccountId, ct);

        if (sourceAccount is not null && destinationAccount is not null
            && IsCrossCurrencyTransfer(sourceAccount, destinationAccount))
        {
            _logger.LogWarning(
                "Transfer failed. FromAccountId: {FromAccountId}, ToAccountId: {ToAccountId}, Reason: Cross-currency transfer ({SourceCurrency} -> {DestinationCurrency})",
                command.FromAccountId, command.ToAccountId, sourceAccount.Currency, destinationAccount.Currency);
            return Result.Failure(
                $"Cross-currency transfers are not supported. Source account currency: {sourceAccount.Currency}, destination account currency: {destinationAccount.Currency}.");
        }

        var withdrawResult = await _dispatcher.DispatchAsync<WithdrawCommand, Result<MoneyDto>>(
            new WithdrawCommand(command.FromAccountId, new Money(command.Amount), command.Description,
                RelatedAccountId: command.ToAccountId),
            ct, saveChanges: false);
        if (!withdrawResult.IsSuccess)
        {
            _logger.LogWarning(
                "Transfer failed. FromAccountId: {FromAccountId}, ToAccountId: {ToAccountId}, Reason: {Reason}",
                command.FromAccountId, command.ToAccountId, string.Join(", ", withdrawResult.Errors));
            return Result.Failure(withdrawResult.Errors);
        }

        var depositResult = await _dispatcher.DispatchAsync<DepositCommand, Result<MoneyDto>>(
            new DepositCommand(command.ToAccountId, new Money(command.Amount), command.Description,
                RelatedAccountId: command.FromAccountId),
            ct, saveChanges: false);
        if (!depositResult.IsSuccess)
        {
            _logger.LogWarning(
                "Transfer failed. FromAccountId: {FromAccountId}, ToAccountId: {ToAccountId}, Reason: {Reason}",
                command.FromAccountId, command.ToAccountId, string.Join(", ", depositResult.Errors));
            return Result.Failure(depositResult.Errors);
        }

        if (saveChanges)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Transfer succeeded. FromAccountId: {FromAccountId}, ToAccountId: {ToAccountId}, Amount: {Amount} {Currency}",
            command.FromAccountId, command.ToAccountId, command.Amount.Amount, command.Amount.Currency);
        return Result.Success();
    }

    private static bool IsCrossCurrencyTransfer(Account source, Account destination)
        => source.Currency != destination.Currency;
}