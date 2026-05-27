using BankingService.Application.Commands.CreateAccount;
using BankingService.Application.Commands.Deposit;
using BankingService.Application.Commands.Transfer;
using BankingService.Application.Commands.Withdraw;
using BankingService.Api.Middleware;
using BankingService.Application.Queries.GetAccountBalance;
using BankingService.Application.Queries.GetAccountDetails;
using BankingService.Application.Services;
using BankingService.Domain.Enums;
using BankingService.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace BankingService.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/accounts")
            .WithTags("Accounts");

        group.MapPost("/", CreateAccount)
            .WithSummary("Create account")
            .WithDescription("Opens a new bank account with an initial deposit.");

        group.MapGet("/{accountId:guid}", GetAccountDetails)
            .WithSummary("Get account details")
            .WithDescription("Returns full account details including current balance.");

        group.MapGet("/{accountId:guid}/balance", GetAccountBalance)
            .WithSummary("Get account balance")
            .WithDescription("Returns current balance only.");

        group.MapPost("/{accountId:guid}/deposits", Deposit)
            .WithSummary("Deposit")
            .WithDescription("Deposits money into an account and returns the new balance.");

        group.MapPost("/{accountId:guid}/withdrawals", Withdraw)
            .WithSummary("Withdraw")
            .WithDescription("Withdraws money from an account and returns the new balance.");

        group.MapPost("/{fromAccountId:guid}/transfers", Transfer)
            .WithSummary("Transfer")
            .WithDescription("Transfers money between two accounts.");

        return app;
    }

    private static async Task<IResult> CreateAccount([FromBody] CreateAccountRequest request,
        IAccountService accountService, CancellationToken ct)
    {
        var command = new CreateAccountCommand(request.FirstName, request.LastName,
            new Money(request.InitialDeposit, request.Currency));
        var result = await accountService.CreateAccountAsync(command, ct);

        return result.IsSuccess
            ? Results.Created($"/api/v1/accounts/{result.Value}", new { AccountId = result.Value })
            : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
    }

    private static async Task<IResult> GetAccountDetails(Guid accountId,
        IAccountService accountService, CancellationToken ct)
    {
        var result = await accountService.GetAccountDetailsAsync(new GetAccountDetailsQuery(accountId), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
    }

    private static async Task<IResult> GetAccountBalance(Guid accountId,
        IAccountService accountService, CancellationToken ct)
    {
        var result = await accountService.GetAccountBalanceAsync(new GetAccountBalanceQuery(accountId), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
    }

    private static async Task<IResult> Deposit(Guid accountId, [FromBody] DepositRequest request,
        IAccountService accountService, CancellationToken ct)
    {
        var command = new DepositCommand(accountId, new Money(request.Amount, request.Currency));
        var result = await accountService.DepositAsync(command, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
    }

    private static async Task<IResult> Withdraw(Guid accountId, [FromBody] WithdrawRequest request,
        IAccountService accountService, CancellationToken ct)
    {
        var command = new WithdrawCommand(accountId, new Money(request.Amount, request.Currency));
        var result = await accountService.WithdrawAsync(command, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
    }

    private static async Task<IResult> Transfer(Guid fromAccountId, [FromBody] TransferRequest request,
        IAccountService accountService, CancellationToken ct)
    {
        var command = new TransferCommand(fromAccountId, request.ToAccountId,
            new Money(request.Amount, request.Currency), request.Description);
        var result = await accountService.TransferAsync(command, ct);

        return result.IsSuccess
            ? Results.Ok()
            : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
    }
    
    private sealed record CreateAccountRequest(
        string FirstName,
        string LastName,
        decimal InitialDeposit,
        Currency Currency);
    
    private sealed record DepositRequest(decimal Amount, Currency Currency);

    private sealed record WithdrawRequest(decimal Amount, Currency Currency);

    private sealed record TransferRequest(
        Guid ToAccountId,
        decimal Amount,
        Currency Currency,
        string? Description = null);
}