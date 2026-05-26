using BankingService.Application;
using BankingService.Api.Endpoints;
using BankingService.Api.Extensions;
using BankingService.Api.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.UseMiddleware<ExceptionMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "BankingService API";
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.MapHealthChecks("/health");

app.MapAccountEndpoints();
app.MapTransactionEndpoints();

app.Run();