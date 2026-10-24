namespace Battleship.Api;

/// <summary>Sent to both players when the game ends. <paramref name="Winner"/> is "you" or "opponent" for the one who receives it.</summary>
internal sealed record GameEndedEvent(string Winner, GameView View);
