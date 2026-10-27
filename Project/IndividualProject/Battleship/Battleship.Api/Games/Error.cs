namespace Battleship.Api;

public sealed record Error(ErrorType Type, string Detail)
{
    public static Error Validation(string detail) => new(ErrorType.Validation, detail);

    public static Error Unauthorized(string detail) => new(ErrorType.Unauthorized, detail);

    public static Error NotFound(string detail) => new(ErrorType.NotFound, detail);

    public static Error Conflict(string detail) => new(ErrorType.Conflict, detail);

    public static Error TooManyRequests(string detail) => new(ErrorType.TooManyRequests, detail);
}
