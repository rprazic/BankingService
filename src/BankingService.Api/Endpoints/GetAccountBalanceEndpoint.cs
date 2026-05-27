using BankingService.Api.Models;
using BankingService.Application.DTOs;
using BankingService.Application.Queries.GetAccountBalance;
using BankingService.Application.Services;

namespace BankingService.Api.Endpoints;

public static class GetAccountBalanceEndpoint
{
    public static IEndpointRouteBuilder MapGetAccountBalance(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/accounts/{accountId:guid}/balance", Handle)
            .WithTags("Accounts")
            .WithSummary("Get account balance")
            .WithDescription("Returns current balance only.")
            .Produces<AccountBalanceDto>()
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(Guid accountId,
        IAccountService accountService, CancellationToken ct)
    {
        var result = await accountService.GetAccountBalanceAsync(new GetAccountBalanceQuery(accountId), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
    }
}
