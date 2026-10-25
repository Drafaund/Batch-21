namespace Battleship.Api;

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
