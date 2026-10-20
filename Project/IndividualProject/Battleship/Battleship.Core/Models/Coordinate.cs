namespace Battleship.Core;

/// <summary>A board position. X is the column and Y is the row, both zero-based.</summary>
public readonly record struct Coordinate
{
    public Coordinate(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }
}
