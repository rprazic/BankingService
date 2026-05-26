using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Application.DTOs;
using BankingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingService.Application.Queries.GetAccountDetails;

public class GetAccountDetailsQueryHandler : IQueryHandler<GetAccountDetailsQuery, Result<AccountDto>>
{
    private readonly BankingDbContext _context;

    public GetAccountDetailsQueryHandler(BankingDbContext context) => _context = context;

    public async Task<Result<AccountDto>> HandleAsync(GetAccountDetailsQuery query, CancellationToken ct)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == query.AccountId, ct);

        return account switch
        {
            null => Result<AccountDto>.Failure("Account not found."),
            _ => Result<AccountDto>.Success(new AccountDto(account.AccountId, account.FirstName, account.LastName,
                account.Iban, new MoneyDto(account.Balance.Amount, account.Balance.Currency), account.CreatedAt))
        };
    }
}