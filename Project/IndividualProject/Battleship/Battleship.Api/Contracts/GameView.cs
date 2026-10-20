namespace Battleship.Api;

/// <summary>Everything one player is allowed to know about a game.</summary>
/// <param name="Version">Grows with every change, so a client can tell a newer view from an older one.</param>
/// <param name="Winner">"you", "opponent", or null while the game is not finished.</param>
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
    int EnemyShipsRemaining);
