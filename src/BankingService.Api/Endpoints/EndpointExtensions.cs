namespace BankingService.Api.Endpoints;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapBankingEndpoints(this IEndpointRouteBuilder app)
    {
        var mutations = app.MapGroup("").RequireRateLimiting("mutations");

        mutations.MapCreateAccount();
        mutations.MapDeposit();
        mutations.MapWithdraw();
        mutations.MapTransfer();

        app.MapGetAccountDetails();
        app.MapGetAccountBalance();
        app.MapGetAccountTransactions();

        return app;
    }
}
