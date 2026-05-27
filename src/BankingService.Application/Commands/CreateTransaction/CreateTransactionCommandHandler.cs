using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.Mappings;
using BankingService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace BankingService.Application.Commands.CreateTransaction;

public class CreateTransactionCommandHandler : ICommandHandler<CreateTransactionCommand, Result<Guid>>
{
    private readonly BankingDbContext _context;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;

    public CreateTransactionCommandHandler(BankingDbContext context, ILogger<CreateTransactionCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(CreateTransactionCommand command, CancellationToken ct,
        bool saveChanges = true)
    {
        var transaction = CreateTransactionMapper.ToEntity(command);
        _context.Transactions.Add(transaction);

        if (saveChanges)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Transaction created. TransactionId: {TransactionId}, AccountId: {AccountId}, Type: {Type}, Amount: {Amount} {Currency}",
            transaction.TransactionId, command.AccountId, command.Type, command.Amount.Amount, command.Amount.Currency);
        return Result<Guid>.Success(transaction.TransactionId);
    }
}