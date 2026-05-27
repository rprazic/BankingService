using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Domain.ValueObjects;

namespace BankingService.Application.Commands.Transfer;

public record TransferCommand(
    Guid FromAccountId,
    Guid ToAccountId,
    Money Amount
) : ICommand<Result>;
