using BankingService.Application.Commands.Deposit;
using BankingService.Application.Commands.Withdraw;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Domain.ValueObjects;
using BankingService.Infrastructure.Persistence;
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
        var withdrawResult = await _dispatcher.DispatchAsync<WithdrawCommand, Result<MoneyDto>>(
            new WithdrawCommand(command.FromAccountId, new Money(command.Amount), command.Description),
            ct, saveChanges: false);
        if (!withdrawResult.IsSuccess)
        {
            _logger.LogWarning(
                "Transfer failed. FromAccountId: {FromAccountId}, ToAccountId: {ToAccountId}, Reason: {Reason}",
                command.FromAccountId, command.ToAccountId, string.Join(", ", withdrawResult.Errors));
            return Result.Failure(withdrawResult.Errors);
        }

        var depositResult = await _dispatcher.DispatchAsync<DepositCommand, Result<MoneyDto>>(
            new DepositCommand(command.ToAccountId, new Money(command.Amount), command.Description),
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
}