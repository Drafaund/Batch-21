namespace Battleship.Core;

/// <summary>Phase of a game.</summary>
public enum GameState
{
    /// <summary>Ships are being placed.</summary>
    Setup,
    /// <summary>The players are taking turns.</summary>
    InProgress,
    /// <summary>A player has won.</summary>
    Finished,
}
