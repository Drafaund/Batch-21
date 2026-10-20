using Battleship.Core;

namespace Battleship.Api;

/// <summary>Builds what a player may see. This is the only place that decides which board data leaves the server.</summary>
internal static class ViewMapper
{
    public static GameView Build(
        string code,
        int version,
        GameController controller,
        OnlinePlayer human,
        Player bot,
        IPlayer? winner)
    {
        var state = controller.GetState();
        var humanFleet = controller.GetFleet(human);

        return new GameView(
            code,
            version,
            ToPhase(state),
            new PlayerView(Seat: 1, human.Name),
            new OpponentView(bot.Name, Connected: true),
            YourTurn: state == GameState.InProgress && ReferenceEquals(controller.GetCurrentPlayer(), human),
            winner == null ? null : ReferenceEquals(winner, human) ? "you" : "opponent",
            BuildPlacement(humanFleet),
            BuildOwnBoard(controller.GetBoard(human)),
            humanFleet.Ships.Select(ToShipView).ToList(),
            BuildEnemyBoard(controller.GetBoard(bot)),
            controller.GetRemainingShips(bot));
    }

    private static GamePhase ToPhase(GameState state)
    {
        return state switch
        {
            GameState.Setup => GamePhase.Placing,
            GameState.InProgress => GamePhase.InProgress,
            _ => GamePhase.Finished,
        };
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
                rows[y][x] = board.Cells[x, y].State switch
                {
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
