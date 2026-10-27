namespace Battleship.Api;

public class Result
{
    private static readonly Result Ok = new(null);

    private readonly Error? _error;

    protected Result(Error? error)
    {
        _error = error;
    }

    public bool IsSuccess => _error is null;

    public bool IsFailure => !IsSuccess;

    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => Ok;

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(null)
    {
        _value = value;
    }

    private Result(Error error)
        : base(error)
    {
    }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);

    public Result<TOut> Map<TOut>(Func<T, TOut> map)
    {
        if (IsFailure)
        {
            return Error;
        }

        return map(Value);
    }

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)
    {
        if (IsFailure)
        {
            return Error;
        }

        return bind(Value);
    }
}
