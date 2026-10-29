using Microsoft.AspNetCore.SignalR;

namespace Battleship.Api;

internal sealed class HubGameEventSink : IGameEventSink
{
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger _logger;

    public HubGameEventSink(IHubContext<GameHub> hub, ILogger logger)
    {
        _hub = hub;
        _logger = logger.ForContext<HubGameEventSink>();
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
            _logger.ForContext(LogEvents.GameCodeProperty, code).Event(LogEvents.EventDeliveryFailed).Warning(exception, "Could not send events");
        }
    }
}
