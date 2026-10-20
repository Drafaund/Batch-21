namespace Battleship.Core;

// Rules stage: remaining ships, the winner, and the end of the game.
public partial class GameController
{
    /// <summary>Number of the player's ships that are not sunk yet.</summary>
    /// <exception cref="ArgumentException">The player is not part of this game.</exception>
    public int GetRemainingShips(IPlayer player)
    {
        return GetFleet(player).Ships.Count(ship => !ship.IsSunk);
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
    /// <exception cref="ArgumentException">The winner is not part of this game.</exception>
    /// <exception cref="InvalidOperationException">The game is not in progress.</exception>
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
