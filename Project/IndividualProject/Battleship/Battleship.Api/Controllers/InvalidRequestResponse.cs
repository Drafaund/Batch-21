using Microsoft.AspNetCore.Mvc;

namespace Battleship.Api;

internal static class InvalidRequestResponse
{
    private const string Detail = "The request body is missing, not valid JSON, or has a value of the wrong type.";

    public static IActionResult Create(ActionContext context)
    {
        var http = context.HttpContext;
        var logger = http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Battleship.Api.Errors");
        logger.LogInformation("{Method} {Path} rejected with {Status}: {Detail}", http.Request.Method, http.Request.Path, StatusCodes.Status400BadRequest, Detail);

        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad request",
            Detail = Detail,
        };
        return new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
    }
}
