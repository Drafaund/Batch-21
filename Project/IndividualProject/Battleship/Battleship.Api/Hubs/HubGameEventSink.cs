using Microsoft.AspNetCore.SignalR;

namespace Battleship.Api;

/// <summary>Sends session events to the SignalR group of each seat. Failures are logged; they never reach the player who caused the event.</summary>
internal sealed class HubGameEventSink : IGameEventSink
{
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<HubGameEventSink> _logger;

    public HubGameEventSink(IHubContext<GameHub> hub, ILogger<HubGameEventSink> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public void Publish(string code, IReadOnlyList<SeatEvent> events)
    {
        _ = SendAsync(code, events);
    }

    private async Task SendAsync(string code, IReadOnlyList<SeatEvent> events)
    {
        try
        {
            foreach (var seatEvent in events)
            {
                await _hub.Clients.Group(GameHub.GroupName(code, seatEvent.Seat)).SendAsync(seatEvent.Name, seatEvent.Payload);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Game {Code}: could not send events", code);
        }
    }
}
