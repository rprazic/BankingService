using BankingService.Api.Models;
using BankingService.Application.Commands.Transfer;
using BankingService.Application.Services;
using BankingService.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace BankingService.Api.Endpoints;

public static class TransferEndpoint
{
    public static IEndpointRouteBuilder MapTransfer(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/accounts/{fromAccountId:guid}/transfers", Handle)
            .WithTags("Accounts")
            .WithSummary("Transfer")
            .WithDescription("Transfers money between two accounts.")
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(Guid fromAccountId, [FromBody] TransferRequest request,
        IAccountService accountService, CancellationToken ct)
    {
        var command = new TransferCommand(fromAccountId, request.ToAccountId,
            new Money(request.Amount, request.Currency), request.Description);
        var result = await accountService.TransferAsync(command, ct);

        return result.IsSuccess
            ? Results.Ok()
            : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
    }
}
