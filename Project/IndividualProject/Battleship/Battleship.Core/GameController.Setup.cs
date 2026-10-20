namespace Battleship.Core;

// Setup stage: placing ships and starting the game.
public partial class GameController
{
    private const int MaxRandomPlacementAttempts = 1000;

    /// <summary>Starts the game. Both fleets must be complete (one ship per <see cref="ShipType"/>).</summary>
    /// <exception cref="InvalidOperationException">The game is not in Setup or a fleet is incomplete.</exception>
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

    /// <summary>Checks whether the ship fits; never changes any state.</summary>
    /// <returns>
    /// True when the game is in Setup, the ship type is not in the fleet yet, and every cell of the ship
    /// is inside the board and free. Horizontal ships extend to the right, vertical ships downward.
    /// </returns>
    /// <exception cref="ArgumentException">The player is not part of this game.</exception>
    public bool ValidatePlaceShip(IPlayer player, IShip ship, Coordinate start, Orientation orientation)
    {
        EnsureParticipant(player);
        ArgumentNullException.ThrowIfNull(ship);

        if (State != GameState.Setup)
        {
            return false;
        }

        var ships = player.Fleet.Ships;
        if (ships.Count >= ShipTypeCount || ships.Any(placed => placed.Type == ship.Type))
        {
            return false;
        }

        var board = player.Board;
        return GetShipCoordinates(start, ship.Length, orientation)
            .All(position => IsInside(board, position) && !board.Cells[position.X, position.Y].IsOccupied);
    }

    /// <summary>Places the ship if valid: fills Positions and Orientation, marks the cells, adds it to the fleet.</summary>
    /// <returns>False, with nothing changed, when the placement is not valid.</returns>
    public bool PlaceShip(IPlayer player, IShip ship, Coordinate start, Orientation orientation)
    {
        if (!ValidatePlaceShip(player, ship, start, orientation))
        {
            return false;
        }

        var positions = GetShipCoordinates(start, ship.Length, orientation).ToList();
        foreach (var position in positions)
        {
            var cell = player.Board.Cells[position.X, position.Y];
            cell.State = CellState.Ship;
            cell.OccupyingShip = ship;
        }

        ship.Positions = positions;
        ship.Orientation = orientation;
        player.Fleet.Ships.Add(ship);
        return true;
    }

    /// <summary>Places every given ship at a random valid spot. The same seed gives the same placement.</summary>
    /// <exception cref="InvalidOperationException">A ship could not be placed (for example its type is already in the fleet).</exception>
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
        return player.Fleet.Ships.Select(ship => ship.Type).Distinct().Count() == ShipTypeCount;
    }
}
