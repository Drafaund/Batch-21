namespace Battleship.Api;

/// <summary>Sent to the first player when the second one joins.</summary>
internal sealed record PlayerJoinedEvent(string Name, GameView View);
