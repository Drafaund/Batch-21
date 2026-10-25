namespace Battleship.Api.Tests;

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
