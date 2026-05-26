using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;

namespace BankingService.Application.Queries.GetAccountBalance;

public record GetAccountBalanceQuery(Guid AccountId) : IQuery<Result<AccountBalanceDto>>;
