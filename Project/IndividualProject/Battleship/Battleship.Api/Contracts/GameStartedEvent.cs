namespace Battleship.Api;

/// <summary>Sent to both players when both fleets are complete.</summary>
internal sealed record GameStartedEvent(GameView View);
