namespace Battleship.Api;

/// <summary>An event for one seat. Each seat gets its own payload because each sees its own filtered view.</summary>
internal sealed record SeatEvent(int Seat, string Name, object Payload);
