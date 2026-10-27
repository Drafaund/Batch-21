namespace Battleship.Api;

public sealed record CreateGameRequest(string? PlayerName, OpponentKind? Opponent);
