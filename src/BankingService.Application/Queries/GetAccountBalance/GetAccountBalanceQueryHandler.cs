using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Application.Queries.GetAccountBalance;

public class GetAccountBalanceQueryHandler : IQueryHandler<GetAccountBalanceQuery, Result<AccountBalanceDto>>
{
    private readonly BankingDbContext _context;

    public GetAccountBalanceQueryHandler(BankingDbContext context) => _context = context;

    public async Task<Result<AccountBalanceDto>> HandleAsync(GetAccountBalanceQuery query, CancellationToken ct)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .Select(a => new { a.AccountId, a.Balance })
            .FirstOrDefaultAsync(a => a.AccountId == query.AccountId, ct);

        return account switch
        {
            null => Result<AccountBalanceDto>.Failure("Account not found."),
            _ => Result<AccountBalanceDto>.Success(new AccountBalanceDto(account.AccountId,
                new MoneyDto(account.Balance.Amount, account.Balance.Currency)))
        };
    }
}
