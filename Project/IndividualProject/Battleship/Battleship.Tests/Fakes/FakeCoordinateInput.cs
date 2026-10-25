namespace Battleship.Tests;

using Battleship.Core;

public class FakeCoordinateInput : ICoordinateInput
{
    private readonly Queue<Coordinate> _coordinates;

    public FakeCoordinateInput(params Coordinate[] coordinates)
    {
        _coordinates = new Queue<Coordinate>(coordinates);
    }

    public int ReadCount { get; private set; }

    public Coordinate ReadCoordinate()
    {
        ReadCount++;
        return _coordinates.Dequeue();
    }
}
