namespace Battleship.Core;

public partial class GameController
{
    private const int MaxRandomPlacementAttempts = 1000;

    public void StartGame()
    {
        if (State != GameState.Setup)
        {
            throw new InvalidOperationException("The game has already started.");
        }

        if (!IsFleetComplete(Player1) || !IsFleetComplete(Player2))
        {
            throw new InvalidOperationException("Both fleets must be complete before the game starts.");
        }

        State = GameState.InProgress;
    }

    public bool ValidatePlaceShip(IPlayer player, IShip ship, Coordinate start, Orientation orientation)
    {
        EnsureParticipant(player);
        ArgumentNullException.ThrowIfNull(ship);

        if (State != GameState.Setup)
        {
            return false;
        }

        var ships = Fleet[player].Ships;
        if (ships.Count >= ShipTypeCount || ships.Any(placed => placed.Type == ship.Type))
        {
            return false;
        }

        var board = Board[player];
        var coordinates = GetShipCoordinates(start, ship.Length, orientation);
        var isPlacementValid = coordinates.All(position =>
            IsInside(board, position) && !board.Cells[position.X, position.Y].IsOccupied);

        return isPlacementValid;
    }

    public bool PlaceShip(IPlayer player, IShip ship, Coordinate start, Orientation orientation)
    {
        if (!ValidatePlaceShip(player, ship, start, orientation))
        {
            return false;
        }

        var positions = GetShipCoordinates(start, ship.Length, orientation).ToList();
        foreach (var position in positions)
        {
            var cell = Board[player].Cells[position.X, position.Y];
            cell.State = CellState.Ship;
            cell.OccupyingShip = ship;
        }

        ship.Positions = positions;
        ship.Orientation = orientation;
        Fleet[player].Ships.Add(ship);
        return true;
    }

    public void PlaceShipsRandomly(IPlayer player, IEnumerable<IShip> ships, Random random)
    {
        ArgumentNullException.ThrowIfNull(ships);
        ArgumentNullException.ThrowIfNull(random);

        var boardSize = GetBoard(player).BoardSize;
        foreach (var ship in ships)
        {
            var placed = false;
            for (var attempt = 0; attempt < MaxRandomPlacementAttempts && !placed; attempt++)
            {
                var start = new Coordinate(random.Next(boardSize), random.Next(boardSize));
                var orientation = random.Next(2) == 0 ? Orientation.Vertical : Orientation.Horizontal;
                placed = PlaceShip(player, ship, start, orientation);
            }

            if (!placed)
            {
                throw new InvalidOperationException($"Could not place the {ship.Type} on the board.");
            }
        }
    }

    private static IEnumerable<Coordinate> GetShipCoordinates(Coordinate start, int length, Orientation orientation)
    {
        for (var offset = 0; offset < length; offset++)
        {
            yield return orientation == Orientation.Horizontal
                ? new Coordinate(start.X + offset, start.Y)
                : new Coordinate(start.X, start.Y + offset);
        }
    }

    private bool IsFleetComplete(IPlayer player)
    {
        var placedTypeCount = Fleet[player].Ships.Select(ship => ship.Type).Distinct().Count();
        var isComplete = placedTypeCount == ShipTypeCount;

        return isComplete;
    }
}
