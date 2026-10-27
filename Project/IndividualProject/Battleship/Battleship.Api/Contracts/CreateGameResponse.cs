namespace Battleship.Api;

public sealed record CreateGameResponse(string Code, string PlayerToken, int Seat, GameView View);
