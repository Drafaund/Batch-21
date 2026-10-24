namespace Battleship.Api;

/// <summary>Where a session sends its events, after it has released its lock.</summary>
internal interface IGameEventSink
{
    void Publish(string code, IReadOnlyList<SeatEvent> events);
}
