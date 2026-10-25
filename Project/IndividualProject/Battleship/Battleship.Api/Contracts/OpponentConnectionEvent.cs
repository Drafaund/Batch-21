namespace Battleship.Api;

internal sealed record OpponentConnectionEvent(bool Connected, GameView View);
