namespace BankingService.Application.Common;

public class BankingValidationException(IReadOnlyList<string> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}