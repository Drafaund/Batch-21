namespace Battleship.Api;

public interface IGameEventSink
{
    void Publish(string code, IReadOnlyList<SeatEvent> events);
}
