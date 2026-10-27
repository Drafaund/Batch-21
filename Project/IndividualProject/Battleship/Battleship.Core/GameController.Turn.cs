namespace Battleship.Core;

// Turn stage: asking for a target, resolving the shot, and passing the turn.
public partial class GameController
{
    public ShotResult TakeTurn()
    {
        EnsureInProgress();

        return TakeTurn(CurrentStrategy.SelectTarget());
    }

    public ShotResult TakeTurn(Coordinate target)
    {
        EnsureInProgress();

        var shooter = _currentPlayer;
        var strategy = CurrentStrategy;

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

    public ShotResult ProcessShot(IPlayer shooter, Coordinate target)
    {
        EnsureParticipant(shooter);
        EnsureInProgress();

        var result = ResolveShot(Board[GetOpponent(shooter)], target);
        OnShotResolved?.Invoke(shooter, target, result);
        return result;
    }

    public void SwitchTurn()
    {
        _currentPlayer = GetOpponent(_currentPlayer);
        _turnNumber++;
    }

    private IShotStrategy CurrentStrategy => ReferenceEquals(_currentPlayer, Player1) ? _strategy1 : _strategy2;

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
