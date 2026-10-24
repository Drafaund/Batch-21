namespace Battleship.Api;

/// <summary>Everything one player is allowed to know about a game.</summary>
/// <param name="Version">Grows with every change, so a client can tell a newer view from an older one.</param>
/// <param name="Winner">"you", "opponent", or null while the game is not finished.</param>
/// <param name="EndReason">"Forfeit" when a player stayed away too long, "Timeout" when the player to fire took too long; otherwise null.</param>
/// <param name="TurnSecondsRemaining">Seconds the player to fire has left, counted when the view was built; null outside a battle between two humans.</param>
/// <param name="EnemyShipsRemaining">Only a count; the enemy ships themselves are never described.</param>
internal sealed record GameView(
    string Code,
    int Version,
    GamePhase Phase,
    PlayerView You,
    OpponentView? Opponent,
    bool YourTurn,
    string? Winner,
    PlacementView Placement,
    OwnBoardView OwnBoard,
    IReadOnlyList<ShipView> OwnShips,
    EnemyBoardView EnemyBoard,
    int EnemyShipsRemaining,
    string? EndReason = null,
    int? TurnSecondsRemaining = null);
