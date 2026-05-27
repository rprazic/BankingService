using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Application.Mappings;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Application.Queries.GetAccountDetails;

public class GetAccountDetailsQueryHandler : IQueryHandler<GetAccountDetailsQuery, Result<AccountDto>>
{
    private readonly BankingDbContext _context;

    public GetAccountDetailsQueryHandler(BankingDbContext context) => _context = context;

    public async Task<Result<AccountDto>> HandleAsync(GetAccountDetailsQuery query, CancellationToken ct)
    {
        var dto = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.AccountId == query.AccountId)
            .Select(AccountDtoMapper.Projection())
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result<AccountDto>.Failure("Account not found.")
            : Result<AccountDto>.Success(dto);
    }
}