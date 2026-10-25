using Microsoft.AspNetCore.SignalR;

namespace Battleship.Api;

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
