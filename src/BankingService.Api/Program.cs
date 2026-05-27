using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using BankingService.Api.Endpoints;
using BankingService.Api.Extensions;
using BankingService.Api.Middleware;
using BankingService.Api.Models;
using BankingService.Application;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("mutations", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ErrorResponse(["Too many requests. Please try again later."]), ct);
    };
});

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.UseMiddleware<ExceptionMiddleware>();
app.UseRateLimiter();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "BankingService API";
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.MapHealthChecks("/health");

app.MapBankingEndpoints();

app.Run();
