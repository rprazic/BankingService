using BankingService.Application.Commands.CreateAccount;
using BankingService.Application.Common;
using BankingService.Application.DTOs;
using BankingService.Application.Queries.GetAccountTransactions;

namespace BankingService.Application;

public interface IAccountService
{
    Task<Result<Guid>> CreateAccountAsync(CreateAccountCommand command, CancellationToken ct);

    // TODO: unlock - Deposit
    // Task<Result<decimal>> DepositAsync(DepositCommand command, CancellationToken ct);

    // TODO: unlock - Withdraw
    // Task<Result<decimal>> WithdrawAsync(WithdrawCommand command, CancellationToken ct);

    // TODO: unlock - Transfer
    // Task<Result> TransferAsync(TransferCommand command, CancellationToken ct);

    // TODO: unlock - GetAccountDetails
    // Task<Result<AccountDto>> GetAccountDetailsAsync(GetAccountDetailsQuery query, CancellationToken ct);

    // TODO: unlock - GetAccountBalance
    // Task<Result<AccountBalanceDto>> GetAccountBalanceAsync(GetAccountBalanceQuery query, CancellationToken ct);

    Task<Result<PagedResult<TransactionDto>>> GetAccountTransactionsAsync(GetAccountTransactionsQuery query, CancellationToken ct);
}
