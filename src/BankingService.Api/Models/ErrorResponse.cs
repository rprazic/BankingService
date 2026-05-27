namespace BankingService.Api.Models;

public record ErrorResponse(
    [property: System.ComponentModel.Description("List of error messages describing what went wrong.")]
    IReadOnlyList<string> Errors);