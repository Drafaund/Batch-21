namespace Battleship.Tests;

using Battleship.Core;

/// <summary>Returns a scripted list of coordinates in order, as if a person had typed them.</summary>
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
