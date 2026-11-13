namespace LanguageLearning.Application.Common.Results;

public class Result
{
    protected Result(bool isSuccess, AppError? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException("成功結果不可包含錯誤。", nameof(error));
        }

        if (!isSuccess && error is null)
        {
            throw new ArgumentException("失敗結果必須包含錯誤。", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public AppError? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(AppError error) => new(false, error);

    public static implicit operator Result(AppError error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(true, null)
    {
        _value = value;
    }

    private Result(AppError error)
        : base(false, error)
    {
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("失敗結果沒有值。");

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(AppError error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(AppError error) => Failure(error);
}
