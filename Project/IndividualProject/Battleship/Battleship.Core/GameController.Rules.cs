namespace Battleship.Core;

// Rules stage: remaining ships, the winner, and the end of the game.
public partial class GameController
{
    public int GetRemainingShips(IPlayer player)
    {
        return GetFleet(player).Ships.Count(ship => !ship.IsSunk);
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
        return player.Fleet.Ships.Count > 0 && GetRemainingShips(player) == 0;
    }
}
