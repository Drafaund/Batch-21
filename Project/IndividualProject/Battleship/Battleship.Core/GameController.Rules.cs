namespace Battleship.Core;

public partial class GameController
{
    public int GetRemainingShips(IPlayer player)
    {
        var remainingShips = GetFleet(player).Ships.Count(ship => !ship.IsSunk);

        return remainingShips;
    }

    public IPlayer? CheckWinner()
    {
        if (IsDefeated(Player2))
        {
            return Player1;
        }

        return IsDefeated(Player1) ? Player2 : null;
    }

    public void EndGame(IPlayer winner)
    {
        EnsureParticipant(winner);
        EnsureInProgress();

        State = GameState.Finished;
        OnGameEnded?.Invoke(winner);
    }

    private bool IsDefeated(IPlayer player)
    {
        var hasPlacedShips = Fleet[player].Ships.Count > 0;
        var isDefeated = hasPlacedShips && GetRemainingShips(player) == 0;

        return isDefeated;
    }
}
