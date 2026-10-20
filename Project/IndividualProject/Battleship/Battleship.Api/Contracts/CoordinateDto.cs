using Battleship.Core;

namespace Battleship.Api;

/// <summary>A board position: X is the column and Y is the row, both zero-based.</summary>
internal sealed record CoordinateDto(int X, int Y)
{
    public static CoordinateDto From(Coordinate coordinate) => new(coordinate.X, coordinate.Y);

    public Coordinate ToCoordinate() => new(X, Y);
}
