namespace Battleship.Api;

internal sealed record PlayerJoinedEvent(string Name, GameView View);
