using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Application.Queries.GetAccountTransactions;

public class GetAccountTransactionsQueryHandler
    : IQueryHandler<GetAccountTransactionsQuery, Result<PagedResult<TransactionDto>>>
{
    private readonly BankingDbContext _context;

    public GetAccountTransactionsQueryHandler(BankingDbContext context) => _context = context;

    public async Task<Result<PagedResult<TransactionDto>>> HandleAsync(GetAccountTransactionsQuery query,
        CancellationToken ct)
    {
        var accountExists = await _context.Accounts
            .AsNoTracking()
            .AnyAsync(a => a.AccountId == query.AccountId, ct);

        if (!accountExists)
        {
            return Result<PagedResult<TransactionDto>>.Failure("Account not found.");
        }

        var txQuery = _context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == query.AccountId);

        if (query.FromDate.HasValue)
        {
            txQuery = txQuery.Where(t => t.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            txQuery = txQuery.Where(t => t.CreatedAt <= query.ToDate.Value);
        }

        var totalCount = await txQuery.CountAsync(ct);

        var items = await txQuery
            .OrderByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TransactionDto(
                t.TransactionId,
                t.Type,
                t.Amount.Amount,
                t.Amount.Currency,
                t.RelatedAccountId,
                t.Description,
                t.CreatedAt))
            .ToListAsync(ct);

        return Result<PagedResult<TransactionDto>>.Success(
            new PagedResult<TransactionDto>(items, query.Page, query.PageSize, totalCount));
    }
}