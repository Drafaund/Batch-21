namespace Battleship.Api;

internal interface IGameEventSink
{
    void Publish(string code, IReadOnlyList<SeatEvent> events);
}
