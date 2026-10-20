namespace Battleship.Core;

/// <summary>Outcome of a single shot.</summary>
public enum ShotResult
{
    /// <summary>The shot hit a ship that is still afloat.</summary>
    Hit,
    /// <summary>The shot hit water.</summary>
    Miss,
    /// <summary>The target is outside the board or was already fired at; the shooter keeps the turn.</summary>
    Invalid,
    /// <summary>The shot hit the last intact part of a ship and sank it.</summary>
    Sunk,
}
