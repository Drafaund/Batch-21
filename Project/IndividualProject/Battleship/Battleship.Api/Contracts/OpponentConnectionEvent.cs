namespace Battleship.Api;

public sealed record OpponentConnectionEvent(bool Connected, GameView View);
