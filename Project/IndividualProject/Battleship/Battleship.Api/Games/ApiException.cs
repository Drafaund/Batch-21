namespace Battleship.Api;

/// <summary>An error the API reports to the caller as a problem details response with a fixed status code.</summary>
internal sealed class ApiException : Exception
{
    private ApiException(int statusCode, string title, string detail)
        : base(detail)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public int StatusCode { get; }

    public string Title { get; }

    public static ApiException BadRequest(string detail) => new(StatusCodes.Status400BadRequest, "Bad request", detail);

    public static ApiException Unauthorized(string detail) => new(StatusCodes.Status401Unauthorized, "Unauthorized", detail);

    public static ApiException NotFound(string detail) => new(StatusCodes.Status404NotFound, "Not found", detail);

    public static ApiException Conflict(string detail) => new(StatusCodes.Status409Conflict, "Conflict", detail);
}
