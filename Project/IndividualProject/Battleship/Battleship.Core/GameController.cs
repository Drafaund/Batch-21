namespace Battleship.Core;

/// <summary>Facade for one two-player game: setup, turns, shots, and the winner.</summary>
public class GameController
{
    private const int MaxRandomPlacementAttempts = 1000;

    private readonly IShotStrategy _strategy1;
    private readonly IShotStrategy _strategy2;
    private IPlayer _currentPlayer;
    private int _turnNumber;

    /// <param name="strategy1">Shot strategy of <paramref name="player1"/> (usually the same object).</param>
    /// <param name="strategy2">Shot strategy of <paramref name="player2"/> (usually the same object).</param>
    public GameController(IPlayer player1, IShotStrategy strategy1, IPlayer player2, IShotStrategy strategy2)
    {
        ArgumentNullException.ThrowIfNull(player1);
        ArgumentNullException.ThrowIfNull(strategy1);
        ArgumentNullException.ThrowIfNull(player2);
        ArgumentNullException.ThrowIfNull(strategy2);
        if (ReferenceEquals(player1, player2))
        {
            throw new ArgumentException("Player1 and Player2 must be different players.", nameof(player2));
        }

        Player1 = player1;
        Player2 = player2;
        _strategy1 = strategy1;
        _strategy2 = strategy2;
        _currentPlayer = player1;
        _turnNumber = 1;
        State = GameState.Setup;
    }

    public IPlayer Player1 { get; }
    public IPlayer Player2 { get; }
    public GameState State { get; private set; }

    /// <summary>Raised after every shot, including <see cref="ShotResult.Invalid"/> ones.</summary>
    public event Action<IPlayer, Coordinate, ShotResult>? OnShotResolved;

    /// <summary>Raised once, with the winner, when the game ends.</summary>
    public event Action<IPlayer>? OnGameEnded;

    public IPlayer GetCurrentPlayer() => _currentPlayer;

    public GameState GetState() => State;

    public IBoard GetBoard(IPlayer player)
    {
        EnsureParticipant(player);
        return player.Board;
    }

    public IFleet GetFleet(IPlayer player)
    {
        EnsureParticipant(player);
        return player.Fleet;
    }

    /// <summary>Number of the player's ships that are not sunk yet.</summary>
    public int GetRemainingShips(IPlayer player)
    {
        return GetFleet(player).Ships.Count(ship => !ship.IsSunk);
    }

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

    /// <summary>
    /// Plays one shot for the current player. A valid shot passes the turn, or ends the game when it sinks
    /// the last enemy ship; an <see cref="ShotResult.Invalid"/> shot keeps the turn.
    /// </summary>
    /// <exception cref="InvalidOperationException">The game is not in progress.</exception>
    public ShotResult TakeTurn()
    {
        EnsureInProgress();

        var shooter = _currentPlayer;
        var strategy = ReferenceEquals(shooter, Player1) ? _strategy1 : _strategy2;

        var target = strategy.SelectTarget();
        var result = ProcessShot(shooter, target);
        strategy.ReceiveResult(target, result);

        if (result == ShotResult.Invalid)
        {
            return result;
        }

        var winner = CheckWinner();
        if (winner != null)
        {
            EndGame(winner);
        }
        else
        {
            SwitchTurn();
        }

        return result;
    }

    /// <summary>Resolves a shot from <paramref name="shooter"/> at the opponent's board and raises <see cref="OnShotResolved"/>.</summary>
    /// <exception cref="InvalidOperationException">The game is not in progress.</exception>
    public ShotResult ProcessShot(IPlayer shooter, Coordinate target)
    {
        EnsureParticipant(shooter);
        EnsureInProgress();

        var result = ResolveShot(GetOpponent(shooter).Board, target);
        OnShotResolved?.Invoke(shooter, target, result);
        return result;
    }

    /// <summary>Passes the turn to the other player.</summary>
    public void SwitchTurn()
    {
        _currentPlayer = GetOpponent(_currentPlayer);
        _turnNumber++;
    }

    /// <summary>The player whose opponent has no ship left, or null while both still have ships.</summary>
    public IPlayer? CheckWinner()
    {
        if (IsDefeated(Player2))
        {
            return Player1;
        }

        return IsDefeated(Player1) ? Player2 : null;
    }

    /// <summary>Finishes the game and raises <see cref="OnGameEnded"/> with the winner.</summary>
    /// <exception cref="InvalidOperationException">The game is not in progress.</exception>
    public void EndGame(IPlayer winner)
    {
        EnsureParticipant(winner);
        EnsureInProgress();

        State = GameState.Finished;
        OnGameEnded?.Invoke(winner);
    }

    private static int ShipTypeCount => Enum.GetValues<ShipType>().Length;

    private static bool IsInside(IBoard board, Coordinate position)
    {
        return position.X >= 0 && position.X < board.BoardSize
            && position.Y >= 0 && position.Y < board.BoardSize;
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

    private static ShotResult ResolveShot(IBoard board, Coordinate target)
    {
        if (!IsInside(board, target))
        {
            return ShotResult.Invalid;
        }

        var cell = board.Cells[target.X, target.Y];
        if (cell.State is CellState.Hit or CellState.Miss)
        {
            return ShotResult.Invalid;
        }

        var ship = cell.OccupyingShip;
        if (ship == null)
        {
            cell.State = CellState.Miss;
            return ShotResult.Miss;
        }

        cell.State = CellState.Hit;
        ship.HitCount++;
        return ship.IsSunk ? ShotResult.Sunk : ShotResult.Hit;
    }

    private bool IsFleetComplete(IPlayer player)
    {
        return player.Fleet.Ships.Select(ship => ship.Type).Distinct().Count() == ShipTypeCount;
    }

    private bool IsDefeated(IPlayer player)
    {
        return player.Fleet.Ships.Count > 0 && GetRemainingShips(player) == 0;
    }

    private IPlayer GetOpponent(IPlayer player)
    {
        return ReferenceEquals(player, Player1) ? Player2 : Player1;
    }

    private void EnsureParticipant(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!ReferenceEquals(player, Player1) && !ReferenceEquals(player, Player2))
        {
            throw new ArgumentException("The player is not part of this game.", nameof(player));
        }
    }

    private void EnsureInProgress()
    {
        if (State != GameState.InProgress)
        {
            throw new InvalidOperationException("The game is not in progress.");
        }
    }
}
