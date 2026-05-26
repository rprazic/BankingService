using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;

namespace BankingService.Application.Queries.GetAccountDetails;

public record GetAccountDetailsQuery(Guid AccountId) : IQuery<Result<AccountDto>>;
