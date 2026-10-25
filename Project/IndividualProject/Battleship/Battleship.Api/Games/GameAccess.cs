namespace Battleship.Api;

internal readonly record struct GameAccess(GameSession Session, int Seat);
