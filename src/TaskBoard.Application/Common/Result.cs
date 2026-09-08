namespace TaskBoard.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public IReadOnlyList<string> Errors { get; }

    protected Result(bool isSuccess, string? error, IReadOnlyList<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? (error != null ? [error] : []);
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(IReadOnlyList<string> errors) => new(false, errors.FirstOrDefault(), errors);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value, string? error, IReadOnlyList<string>? errors = null)
        : base(isSuccess, error, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public new static Result<T> Failure(string error) => new(false, default, error);
    public new static Result<T> Failure(IReadOnlyList<string> errors) => new(false, default, errors.FirstOrDefault(), errors);
}
