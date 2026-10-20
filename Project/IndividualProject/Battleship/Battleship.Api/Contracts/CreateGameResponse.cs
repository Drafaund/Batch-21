namespace Battleship.Api;

/// <summary>Answer to a new game: the code to share, the secret token of the player, and the first view.</summary>
internal sealed record CreateGameResponse(string Code, string PlayerToken, int Seat, GameView View);
