namespace Battleship.Tests;

using Battleship.Core;

public class FakeShip : IShip
{
    public FakeShip(ShipType type, int length)
    {
        Type = type;
        Length = length;
    }

    public ShipType Type { get; }

    public int Length { get; }

    public Orientation Orientation { get; set; }

    public List<Coordinate> Positions { get; set; } = new();

    public int HitCount { get; set; }

    public bool IsSunk => HitCount >= Length;
}
