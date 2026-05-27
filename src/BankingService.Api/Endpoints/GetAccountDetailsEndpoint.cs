using BankingService.Api.Models;
using BankingService.Application.DTOs;
using BankingService.Application.Queries.GetAccountDetails;
using BankingService.Application.Services;

namespace BankingService.Api.Endpoints;

public static class GetAccountDetailsEndpoint
{
    public static IEndpointRouteBuilder MapGetAccountDetails(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/accounts/{accountId:guid}", Handle)
            .WithTags("Accounts")
            .WithSummary("Get account details")
            .WithDescription("Returns full account details including current balance.")
            .Produces<AccountDto>()
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(Guid accountId,
        IAccountService accountService, CancellationToken ct)
    {
        var result = await accountService.GetAccountDetailsAsync(new GetAccountDetailsQuery(accountId), ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
    }
}
