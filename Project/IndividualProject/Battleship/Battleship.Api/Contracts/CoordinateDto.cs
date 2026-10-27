using Battleship.Core;

namespace Battleship.Api;

public sealed record CoordinateDto(int X, int Y)
{
    public static CoordinateDto From(Coordinate coordinate) => new(coordinate.X, coordinate.Y);

    public Coordinate ToCoordinate() => new(X, Y);
}
