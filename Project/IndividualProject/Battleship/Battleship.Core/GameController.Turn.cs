namespace Battleship.Core;

// Turn stage: asking for a target, resolving the shot, and passing the turn.
public partial class GameController
{
    /// <summary>
    /// Plays one shot for the current player. A valid shot passes the turn, or ends the game when it sinks
    /// the last enemy ship; an <see cref="ShotResult.Invalid"/> shot keeps the turn.
    /// </summary>
    /// <returns>The result of the shot.</returns>
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
    /// <returns>
    /// <see cref="ShotResult.Invalid"/> for a target outside the board or already fired at; otherwise
    /// <see cref="ShotResult.Miss"/>, <see cref="ShotResult.Hit"/>, or <see cref="ShotResult.Sunk"/>.
    /// </returns>
    /// <exception cref="ArgumentException">The shooter is not part of this game.</exception>
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
}
