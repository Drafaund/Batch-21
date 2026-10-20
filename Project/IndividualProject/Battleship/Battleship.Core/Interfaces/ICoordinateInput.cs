namespace Battleship.Core;

/// <summary>Source of coordinates typed or clicked by a human.</summary>
public interface ICoordinateInput
{
    /// <summary>Waits until the human gives a coordinate and returns it.</summary>
    Coordinate ReadCoordinate();
}
