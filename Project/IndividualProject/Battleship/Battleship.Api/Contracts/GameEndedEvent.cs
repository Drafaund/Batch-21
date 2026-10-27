namespace Battleship.Api;

public sealed record GameEndedEvent(string Winner, GameView View);
