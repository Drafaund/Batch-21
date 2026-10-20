namespace Battleship.Api;

/// <summary>
/// Turns the errors the API raises on purpose (<see cref="ApiException"/>) and unreadable requests into
/// problem details responses. They are expected, so they are logged as information, not as errors.
/// Any other exception passes on to the exception handler, which answers 500.
/// </summary>
internal sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, IProblemDetailsService problemDetails, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (exception is ApiException or BadHttpRequestException)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (status, title, detail) = Describe(exception);
            _logger.LogInformation("{Method} {Path} rejected with {Status}: {Detail}", context.Request.Method, context.Request.Path, status, detail);

            context.Response.StatusCode = status;
            await _problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = exception,
                ProblemDetails = { Status = status, Title = title, Detail = detail },
            });
        }
    }

    private static (int Status, string Title, string Detail) Describe(Exception exception)
    {
        return exception switch
        {
            ApiException api => (api.StatusCode, api.Title, api.Message),
            BadHttpRequestException bad => (bad.StatusCode, "Bad request", "The request body is missing, not valid JSON, or has a value of the wrong type."),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "An unexpected error occurred."),
        };
    }
}
