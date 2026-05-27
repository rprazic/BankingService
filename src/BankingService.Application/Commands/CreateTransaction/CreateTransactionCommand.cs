using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;

namespace BankingService.Application.Commands.CreateTransaction;

public record CreateTransactionCommand(
    Guid AccountId,
    TransactionType Type,
    Money Amount,
    DateTime CreatedAt,
    string? Description = null,
    Guid? RelatedAccountId = null
) : ICommand<Result<Guid>>;
