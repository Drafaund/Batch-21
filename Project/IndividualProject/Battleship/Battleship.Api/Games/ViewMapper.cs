using Battleship.Core;

namespace Battleship.Api;

internal static class ViewMapper
{
    public static GameView Build(
        string code,
        int version,
        GameController controller,
        Seat viewer,
        Seat opponent,
        GamePhase phase,
        IPlayer? winner,
        string? endReason,
        int? turnSecondsRemaining)
    {
        var state = controller.GetState();
        var ownFleet = controller.GetFleet(viewer.Player);
        var started = phase is GamePhase.InProgress or GamePhase.Finished;

        return new GameView(
            code,
            version,
            phase,
            new PlayerView(viewer.Number, viewer.Player.Name),
            phase == GamePhase.Lobby ? null : new OpponentView(opponent.Player.Name, opponent.IsConnected),
            YourTurn: state == GameState.InProgress && ReferenceEquals(controller.GetCurrentPlayer(), viewer.Player),
            winner == null ? null : ReferenceEquals(winner, viewer.Player) ? "you" : "opponent",
            BuildPlacement(ownFleet),
            BuildOwnBoard(controller.GetBoard(viewer.Player)),
            ownFleet.Ships.Select(ToShipView).ToList(),
            BuildEnemyBoard(controller.GetBoard(opponent.Player)),
            // Before the battle the other fleet may be incomplete, so the count would reveal how far they got.
            started ? controller.GetRemainingShips(opponent.Player) : Enum.GetValues<ShipType>().Length,
            endReason,
            turnSecondsRemaining);
    }

    private static PlacementView BuildPlacement(IFleet fleet)
    {
        var placed = fleet.Ships.Select(ship => ship.Type).ToList();
        var remaining = Enum.GetValues<ShipType>().Where(type => !placed.Contains(type)).ToList();
        return new PlacementView(placed, remaining);
    }

    private static OwnBoardView BuildOwnBoard(IBoard board)
    {
        var rows = new OwnCell[board.BoardSize][];
        for (var y = 0; y < board.BoardSize; y++)
        {
            rows[y] = new OwnCell[board.BoardSize];
            for (var x = 0; x < board.BoardSize; x++)
            {
                rows[y][x] = board.Cells[x, y].State switch
                {
                    CellState.Ship => OwnCell.Ship,
                    CellState.Hit => OwnCell.Hit,
                    CellState.Miss => OwnCell.Miss,
                    _ => OwnCell.Water,
                };
            }
        }

        return new OwnBoardView(board.BoardSize, rows);
    }

    private static EnemyBoardView BuildEnemyBoard(IBoard board)
    {
        var rows = new EnemyCell[board.BoardSize][];
        for (var y = 0; y < board.BoardSize; y++)
        {
            rows[y] = new EnemyCell[board.BoardSize];
            for (var x = 0; x < board.BoardSize; x++)
            {
                // A cell that holds a ship but was not hit yet is reported as Unknown, like water.
                // A hit cell of a ship that has sunk is Sunk: every cell of that ship is already hit, so nothing is revealed.
                var cell = board.Cells[x, y];
                rows[y][x] = cell.State switch
                {
                    CellState.Hit when cell.OccupyingShip is { IsSunk: true } => EnemyCell.Sunk,
                    CellState.Hit => EnemyCell.Hit,
                    CellState.Miss => EnemyCell.Miss,
                    _ => EnemyCell.Unknown,
                };
            }
        }

        return new EnemyBoardView(board.BoardSize, rows);
    }

    private static ShipView ToShipView(IShip ship)
    {
        return new ShipView(
            ship.Type,
            ship.Length,
            ship.Orientation,
            ship.Positions.Select(CoordinateDto.From).ToList(),
            ship.HitCount,
            ship.IsSunk);
    }
}
