using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Domain.ValueObjects;

namespace BankingService.Application.Commands.Deposit;

public record DepositCommand(
    Guid AccountId,
    Money Amount,
    string? Description = null,
    Guid? RelatedAccountId = null
) : ICommand<Result<MoneyDto>>;
