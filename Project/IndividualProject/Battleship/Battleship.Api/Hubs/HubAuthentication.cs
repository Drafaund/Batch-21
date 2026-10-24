using Microsoft.Extensions.Primitives;

namespace Battleship.Api;

/// <summary>
/// Checks the room code and the player token before a SignalR connection is negotiated, so a wrong token
/// gets the same 401 or 404 problem details as the REST endpoints. Browsers cannot set headers on a WebSocket,
/// so the token also comes as the <c>access_token</c> query value, which is what the SignalR client sends.
/// </summary>
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
                GameHub.RememberAccess(context, access);
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
