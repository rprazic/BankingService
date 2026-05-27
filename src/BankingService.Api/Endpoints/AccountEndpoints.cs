using System.ComponentModel;
using BankingService.Application.Commands.CreateAccount;
using BankingService.Application.Commands.Deposit;
using BankingService.Application.Commands.Transfer;
using BankingService.Application.Commands.Withdraw;
using BankingService.Api.Middleware;
using BankingService.Application.DTOs;
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
            .WithDescription("Opens a new bank account with an initial deposit.")
            .Produces<CreateAccountResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);


        group.MapGet("/{accountId:guid}", GetAccountDetails)
            .WithSummary("Get account details")
            .WithDescription("Returns full account details including current balance.")
            .Produces<AccountDto>()
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("/{accountId:guid}/balance", GetAccountBalance)
            .WithSummary("Get account balance")
            .WithDescription("Returns current balance only.")
            .Produces<AccountBalanceDto>()
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapPost("/{accountId:guid}/deposits", Deposit)
            .WithSummary("Deposit")
            .WithDescription("Deposits money into an account and returns the new balance.")
            .Produces<MoneyDto>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapPost("/{accountId:guid}/withdrawals", Withdraw)
            .WithSummary("Withdraw")
            .WithDescription("Withdraws money from an account and returns the new balance.")
            .Produces<MoneyDto>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapPost("/{fromAccountId:guid}/transfers", Transfer)
            .WithSummary("Transfer")
            .WithDescription("Transfers money between two accounts.")
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> CreateAccount([FromBody] CreateAccountRequest request,
        IAccountService accountService, CancellationToken ct)
    {
        var command = new CreateAccountCommand(request.FirstName, request.LastName,
            new Money(request.InitialDeposit, request.Currency));
        var result = await accountService.CreateAccountAsync(command, ct);

        return result.IsSuccess
            ? Results.Created($"/api/v1/accounts/{result.Value}", new CreateAccountResponse(result.Value))
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

    private sealed record CreateAccountResponse(
        [property: Description("Newly created account's unique identifier.")]
        Guid AccountId);

    private sealed record CreateAccountRequest(
        [property: Description("Account holder's first name. Max 100 characters.")]
        string FirstName,
        [property: Description("Account holder's last name. Max 100 characters.")]
        string LastName,
        [property: Description("Initial deposit amount. Must be 0 or greater.")]
        decimal InitialDeposit,
        [property: Description("Account currency as ISO 4217 numeric code (978 = EUR, 840 = USD, 941 = RSD).")]
        Currency Currency);

    private sealed record DepositRequest(
        [property: Description("Amount to deposit. Must be greater than 0.")]
        decimal Amount,
        [property: Description("Currency of the deposit. Must match the account's currency.")]
        Currency Currency);

    private sealed record WithdrawRequest(
        [property: Description("Amount to withdraw. Must be greater than 0.")]
        decimal Amount,
        [property: Description("Currency of the withdrawal. Must match the account's currency.")]
        Currency Currency);

    private sealed record TransferRequest(
        [property: Description("Destination account identifier.")]
        Guid ToAccountId,
        [property: Description("Amount to transfer. Must be greater than 0.")]
        decimal Amount,
        [property: Description("Currency of the transfer. Must match both accounts' currency.")]
        Currency Currency,
        [property: Description("Optional free-text note, e.g. 'Rent payment'.")]
        string? Description = null);
}