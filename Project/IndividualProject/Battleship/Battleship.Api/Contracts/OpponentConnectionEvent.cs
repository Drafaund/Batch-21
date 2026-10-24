namespace Battleship.Api;

/// <summary>Sent when the opponent connects or drops.</summary>
internal sealed record OpponentConnectionEvent(bool Connected, GameView View);
