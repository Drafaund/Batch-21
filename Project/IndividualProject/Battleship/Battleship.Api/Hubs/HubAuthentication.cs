using Microsoft.Extensions.Primitives;

namespace Battleship.Api;

internal static class HubAuthentication
{
    private const string BearerPrefix = "Bearer ";

    public static IApplicationBuilder UseHubAuthentication(this IApplicationBuilder app)
    {
        return app.UseWhen(
            context => context.Request.Path.StartsWithSegments(GameHub.Path),
            branch => branch.Use(async (context, next) =>
            {
                var store = context.RequestServices.GetRequiredService<GameStore>();
                var access = store.GetAuthorized(context.Request.Query["code"].ToString(), ReadToken(context.Request));
                if (access.IsFailure)
                {
                    await access.Error.ToProblem(context).ExecuteAsync(context);
                    return;
                }

                GameHub.RememberAccess(context, access.Value);
                await next();
            }));
    }

    private static string? ReadToken(HttpRequest request)
    {
        if (request.Query.TryGetValue("access_token", out var fromQuery) && !StringValues.IsNullOrEmpty(fromQuery))
        {
            return fromQuery.ToString();
        }

        var header = request.Headers.Authorization.ToString();
        return header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase) ? header[BearerPrefix.Length..] : null;
    }
}
