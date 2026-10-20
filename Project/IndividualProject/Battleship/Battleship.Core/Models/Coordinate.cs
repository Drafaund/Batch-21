namespace Battleship.Core;

/// <summary>A board position. X is the column and Y is the row, both zero-based.</summary>
public readonly record struct Coordinate
{
    /// <summary>Creates a coordinate. The values are not range checked, so a shot outside the board can be represented.</summary>
    public Coordinate(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>The zero-based column.</summary>
    public int X { get; }
    /// <summary>The zero-based row.</summary>
    public int Y { get; }
}
