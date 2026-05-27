using BankingService.Application.Commands.Deposit;
using BankingService.Application.Commands.Withdraw;
using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Domain.ValueObjects;
using BankingService.Infrastructure.Persistence;

namespace BankingService.Application.Commands.Transfer;

public class TransferCommandHandler : ICommandHandler<TransferCommand, Result>
{
    private readonly BankingDbContext _context;
    private readonly ICommandDispatcher _dispatcher;

    public TransferCommandHandler(BankingDbContext context, ICommandDispatcher dispatcher)
    {
        _context = context;
        _dispatcher = dispatcher;
    }

    public async Task<Result> HandleAsync(TransferCommand command, CancellationToken ct, bool saveChanges = true)
    {
        var withdrawResult = await _dispatcher.DispatchAsync<WithdrawCommand, Result<MoneyDto>>(
            new WithdrawCommand(command.FromAccountId, new Money(command.Amount)),
            ct, saveChanges: false);
        if (!withdrawResult.IsSuccess)
        {
            return Result.Failure(withdrawResult.Errors);
        }

        var depositResult = await _dispatcher.DispatchAsync<DepositCommand, Result<MoneyDto>>(
            new DepositCommand(command.ToAccountId, new Money(command.Amount)),
            ct, saveChanges: false);
        if (!depositResult.IsSuccess)
        {
            return Result.Failure(depositResult.Errors);
        }

        if (saveChanges)
            await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
