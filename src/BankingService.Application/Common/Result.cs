namespace BankingService.Application.Common;

public class Result
{
    protected Result(bool isSuccess, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }

    public static Result Success() => new(true, []);
    public static Result Failure(string error) => new(false, [error]);
    public static Result Failure(IReadOnlyList<string> errors) => new(false, errors);
}