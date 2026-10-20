namespace Battleship.Api;

/// <summary>The other side of the game.</summary>
internal sealed record OpponentView(string Name, bool Connected);
