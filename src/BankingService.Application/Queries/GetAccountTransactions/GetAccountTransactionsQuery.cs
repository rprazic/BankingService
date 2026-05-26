using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;

namespace BankingService.Application.Queries.GetAccountTransactions;

public record GetAccountTransactionsQuery(
    Guid AccountId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20
) : PagedQuery(Page, PageSize), IQuery<Result<PagedResult<TransactionDto>>>;
