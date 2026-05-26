using BankingService.Application;
using BankingService.Application.Queries.GetAccountTransactions;
using BankingService.Api.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace BankingService.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/accounts/{accountId:guid}/transactions")
            .WithTags("Transactions");

        group.MapGet("/", GetTransactions)
            .WithSummary("Get transaction history")
            .WithDescription("Returns a paginated list of transactions for the given account, with optional date filters.");

        return app;
    }

    private static async Task<IResult> GetTransactions(
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
