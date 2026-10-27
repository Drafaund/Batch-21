using Microsoft.AspNetCore.Mvc;

namespace Battleship.Api;

internal static class ResultHttpExtensions
{
    public static ActionResult<T> ToActionResult<T>(this Result<T> result, HttpContext http)
    {
        if (result.IsFailure)
        {
            return result.Error.ToActionResult(http);
        }

        return result.Value;
    }

    public static ActionResult ToActionResult(this Result result, HttpContext http)
    {
        if (result.IsFailure)
        {
            return result.Error.ToActionResult(http);
        }

        return new NoContentResult();
    }

    public static ActionResult ToActionResult(this Error error, HttpContext http)
    {
        var problem = error.ToProblemDetails(http);
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }

    public static IResult ToProblem(this Error error, HttpContext http)
    {
        return Results.Problem(error.ToProblemDetails(http));
    }

    private static ProblemDetails ToProblemDetails(this Error error, HttpContext http)
    {
        var (status, title) = Describe(error.Type);
        var logger = http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Battleship.Api.Errors");
        logger.LogInformation("{Method} {Path} rejected with {Status}: {Detail}", http.Request.Method, http.Request.Path, status, error.Detail);
        return new ProblemDetails { Status = status, Title = title, Detail = error.Detail };
    }

    private static (int Status, string Title) Describe(ErrorType type)
    {
        return type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Bad request"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.TooManyRequests => (StatusCodes.Status429TooManyRequests, "Too many requests"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown error type."),
        };
    }
}
