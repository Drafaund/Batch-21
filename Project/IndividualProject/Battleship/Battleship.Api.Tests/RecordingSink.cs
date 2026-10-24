namespace Battleship.Api.Tests;

/// <summary>Keeps the events a session publishes, so a test can read them without a SignalR connection.</summary>
internal sealed class RecordingSink : IGameEventSink
{
    private readonly object _sync = new();
    private readonly List<SeatEvent> _events = new();

    public IReadOnlyList<SeatEvent> Events
    {
        get
        {
            lock (_sync)
            {
                return _events.ToList();
            }
        }
    }

    public void Publish(string code, IReadOnlyList<SeatEvent> events)
    {
        lock (_sync)
        {
            _events.AddRange(events);
        }
    }
}
