namespace Battleship.Api;

internal sealed record CreateGameResponse(string Code, string PlayerToken, int Seat, GameView View);
