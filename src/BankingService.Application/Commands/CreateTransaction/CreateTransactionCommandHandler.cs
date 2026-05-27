using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Domain.Entities;
using BankingService.Infrastructure.Persistence;

namespace BankingService.Application.Commands.CreateTransaction;

public class CreateTransactionCommandHandler : ICommandHandler<CreateTransactionCommand, Result<Guid>>
{
    private readonly BankingDbContext _context;

    public CreateTransactionCommandHandler(BankingDbContext context) => _context = context;

    public async Task<Result<Guid>> HandleAsync(CreateTransactionCommand command, CancellationToken ct,
        bool saveChanges = true)
    {
        var transaction = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            AccountId = command.AccountId,
            Type = command.Type,
            Amount = command.Amount,
            Description = command.Description,
            RelatedAccountId = command.RelatedAccountId,
            CreatedAt = command.CreatedAt
        };

        _context.Transactions.Add(transaction);

        if (saveChanges)
        {
            await _context.SaveChangesAsync(ct);
        }

        return Result<Guid>.Success(transaction.TransactionId);
    }
}
