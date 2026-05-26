namespace BankingService.Application.Common;

public class Result<T> : Result
{
    private Result(T value) : base(true, []) => Value = value;
    private Result(IReadOnlyList<string> errors) : base(false, errors) => Value = default;

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(string error) => new([error]);
    public new static Result<T> Failure(IReadOnlyList<string> errors) => new(errors);
}