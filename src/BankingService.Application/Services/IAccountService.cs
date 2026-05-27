using BankingService.Application.Commands.CreateAccount;
using BankingService.Application.Commands.Deposit;
using BankingService.Application.Commands.Withdraw;
using BankingService.Application.Common;
using BankingService.Application.DTOs;
using BankingService.Application.Queries.GetAccountBalance;
using BankingService.Application.Queries.GetAccountDetails;
using BankingService.Application.Queries.GetAccountTransactions;

namespace BankingService.Application;

public interface IAccountService
{
    Task<Result<Guid>> CreateAccountAsync(CreateAccountCommand command, CancellationToken ct);

    Task<Result<MoneyDto>> DepositAsync(DepositCommand command, CancellationToken ct);

    Task<Result<MoneyDto>> WithdrawAsync(WithdrawCommand command, CancellationToken ct);

    // TODO: unlock - Transfer
    // Task<Result> TransferAsync(TransferCommand command, CancellationToken ct);

    Task<Result<AccountDto>> GetAccountDetailsAsync(GetAccountDetailsQuery query, CancellationToken ct);

    Task<Result<AccountBalanceDto>> GetAccountBalanceAsync(GetAccountBalanceQuery query, CancellationToken ct);

    Task<Result<PagedResult<TransactionDto>>> GetAccountTransactionsAsync(GetAccountTransactionsQuery query, CancellationToken ct);
}
