namespace Battleship.Api;

/// <summary>Body of <c>POST /api/games/{code}/join</c>. The name is nullable so a missing one gives a clear 400.</summary>
internal sealed record JoinGameRequest(string? PlayerName);
