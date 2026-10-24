using Microsoft.AspNetCore.SignalR;

namespace Battleship.Api;

/// <summary>
/// The real-time channel of a game. It only pushes events; the clients send commands over REST.
/// The token was already checked by <see cref="HubAuthentication"/> before the connection opened.
/// </summary>
internal sealed class GameHub : Hub
{
    public const string Path = "/hubs/game";

    private const string AccessKey = "battleship.access";

    public static string GroupName(string code, int seat) => $"game:{code}:seat{seat}";

    internal static void RememberAccess(HttpContext context, GameAccess access) => context.Items[AccessKey] = access;

    public override async Task OnConnectedAsync()
    {
        var access = GetAccess();
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(access.Session.Code, access.Seat));
        access.Session.Connect(access.Seat);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var access = GetAccess();
        access.Session.Disconnect(access.Seat);
        await base.OnDisconnectedAsync(exception);
    }

    private GameAccess GetAccess()
    {
        // The HTTP context of the request that opened the connection carries what the authentication step found.
        var context = Context.GetHttpContext();
        return context?.Items[AccessKey] is GameAccess access
            ? access
            : throw new HubException("The connection was not authenticated.");
    }
}
