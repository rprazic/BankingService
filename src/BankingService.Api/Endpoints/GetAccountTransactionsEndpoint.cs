using BankingService.Api.Models;
using BankingService.Application.Common;
using BankingService.Application.DTOs;
using BankingService.Application.Queries.GetAccountTransactions;
using BankingService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingService.Api.Endpoints;

public static class GetAccountTransactionsEndpoint
{
    public static IEndpointRouteBuilder MapGetAccountTransactions(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/accounts/{accountId:guid}/transactions", Handle)
            .WithTags("Transactions")
            .WithSummary("Get transaction history")
            .WithDescription("Returns a paginated list of transactions for the given account, with optional date filters.")
            .Produces<PagedResult<TransactionDto>>()
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> Handle(
        Guid accountId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IAccountService accountService,
        CancellationToken ct)
    {
        var query = new GetAccountTransactionsQuery(
            accountId,
            FromDate: fromDate,
            ToDate: toDate,
            Page: page == 0 ? 1 : page,
            PageSize: pageSize == 0 ? 20 : pageSize);

        var result = await accountService.GetAccountTransactionsAsync(query, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.UnprocessableEntity(new ErrorResponse(result.Errors));
    }
}
