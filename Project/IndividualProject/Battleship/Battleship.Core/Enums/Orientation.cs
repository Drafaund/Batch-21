namespace Battleship.Core;

/// <summary>Direction a ship extends from its start coordinate.</summary>
public enum Orientation
{
    /// <summary>The ship extends downward (increasing Y) from its first cell.</summary>
    Vertical,
    /// <summary>The ship extends to the right (increasing X) from its first cell.</summary>
    Horizontal,
}
