using BankingService.Application.CQRS;
using BankingService.Application.Common;
using BankingService.Domain.Enums;

namespace BankingService.Application.Commands.CreateAccount;

public record CreateAccountCommand(
    string FirstName,
    string LastName,
    decimal InitialDeposit,
    Currency Currency
) : ICommand<Result<Guid>>;