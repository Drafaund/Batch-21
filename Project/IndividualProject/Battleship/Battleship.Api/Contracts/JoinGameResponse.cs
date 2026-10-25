namespace Battleship.Api;

internal sealed record JoinGameResponse(string PlayerToken, int Seat, GameView View);
