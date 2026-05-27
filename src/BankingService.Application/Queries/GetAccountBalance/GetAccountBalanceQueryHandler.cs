using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Application.Mappings;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Application.Queries.GetAccountBalance;

public class GetAccountBalanceQueryHandler : IQueryHandler<GetAccountBalanceQuery, Result<AccountBalanceDto>>
{
    private readonly BankingDbContext _context;

    public GetAccountBalanceQueryHandler(BankingDbContext context) => _context = context;

    public async Task<Result<AccountBalanceDto>> HandleAsync(GetAccountBalanceQuery query, CancellationToken ct)
    {
        var dto = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.AccountId == query.AccountId)
            .Select(AccountBalanceDtoMapper.Projection())
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result<AccountBalanceDto>.Failure("Account not found.")
            : Result<AccountBalanceDto>.Success(dto);
    }
}
