using BankingService.Api.Models;
using BankingService.Application.Commands.CreateAccount;
using BankingService.Application.Services;
using BankingService.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace BankingService.Api.Endpoints;

public static class CreateAccountEndpoint
{
    public static IEndpointRouteBuilder MapCreateAccount(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/accounts", Handle)
            .WithTags("Accounts")
            .WithSummary("Create account")
            .WithDescription("Opens a new bank account with an initial deposit.")
            .Produces<CreateAccountResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle([FromBody] CreateAccountRequest request,
        IAccountService accountService, CancellationToken ct)
    {
        var command = new CreateAccountCommand(request.FirstName, request.LastName,
            new Money(request.InitialDeposit, request.Currency));
        var result = await accountService.CreateAccountAsync(command, ct);

        return result.IsSuccess
            ? Results.Created($"/api/v1/accounts/{result.Value}", new CreateAccountResponse(result.Value))
            : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
    }
}
