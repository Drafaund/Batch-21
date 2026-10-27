namespace Battleship.Api;

public sealed record PlayerJoinedEvent(string Name, GameView View);
