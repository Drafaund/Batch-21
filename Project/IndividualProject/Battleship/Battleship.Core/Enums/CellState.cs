namespace Battleship.Core;

/// <summary>State of a single board cell.</summary>
public enum CellState
{
    /// <summary>Water that has not been fired at.</summary>
    Empty,
    /// <summary>Holds an intact part of a ship.</summary>
    Ship,
    /// <summary>A ship part that was hit.</summary>
    Hit,
    /// <summary>Water that was fired at.</summary>
    Miss,
}
