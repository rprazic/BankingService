using BankingService.Api.Models;
using BankingService.Application.Commands.Withdraw;
using BankingService.Application.DTOs;
using BankingService.Application.Services;
using BankingService.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace BankingService.Api.Endpoints;

public static class WithdrawEndpoint
{
    public static IEndpointRouteBuilder MapWithdraw(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/accounts/{accountId:guid}/withdrawals", Handle)
            .WithTags("Accounts")
            .WithSummary("Withdraw")
            .WithDescription("Withdraws money from an account and returns the new balance.")
            .Produces<MoneyDto>()
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(Guid accountId, [FromBody] WithdrawRequest request,
        IAccountService accountService, CancellationToken ct)
    {
        var command = new WithdrawCommand(accountId, new Money(request.Amount, request.Currency));
        var result = await accountService.WithdrawAsync(command, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
    }
}
