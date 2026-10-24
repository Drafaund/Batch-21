namespace Battleship.Api;

/// <summary>A game together with the seat a valid token belongs to.</summary>
internal readonly record struct GameAccess(GameSession Session, int Seat);
