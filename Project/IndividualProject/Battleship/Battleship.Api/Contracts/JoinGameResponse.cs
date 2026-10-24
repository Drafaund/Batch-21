namespace Battleship.Api;

/// <summary>Answer to a join: the secret token of the new player, their seat, and the first view.</summary>
internal sealed record JoinGameResponse(string PlayerToken, int Seat, GameView View);
