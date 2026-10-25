namespace Battleship.Api;

internal sealed record CreateGameRequest(string? PlayerName, OpponentKind? Opponent);
