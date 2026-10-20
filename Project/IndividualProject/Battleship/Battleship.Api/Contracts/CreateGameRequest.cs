namespace Battleship.Api;

/// <summary>Body of <c>POST /api/games</c>. Members are nullable so a missing one gives a clear 400.</summary>
internal sealed record CreateGameRequest(string? PlayerName, OpponentKind? Opponent);
