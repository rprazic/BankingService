using BankingService.Application.CQRS;
using BankingService.Application.Common;
using BankingService.Domain.ValueObjects;

namespace BankingService.Application.Commands.CreateAccount;

public record CreateAccountCommand(
    string FirstName,
    string LastName,
    Money InitialDeposit
) : ICommand<Result<Guid>>;