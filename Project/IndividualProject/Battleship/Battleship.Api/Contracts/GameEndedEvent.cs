namespace Battleship.Api;

internal sealed record GameEndedEvent(string Winner, GameView View);
