namespace Battleship.Api;

public sealed record JoinGameResponse(string PlayerToken, int Seat, GameView View);
