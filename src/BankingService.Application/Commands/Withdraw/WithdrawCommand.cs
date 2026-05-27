using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Domain.ValueObjects;

namespace BankingService.Application.Commands.Withdraw;

public record WithdrawCommand(
    Guid AccountId,
    Money Amount
) : ICommand<Result<MoneyDto>>;
