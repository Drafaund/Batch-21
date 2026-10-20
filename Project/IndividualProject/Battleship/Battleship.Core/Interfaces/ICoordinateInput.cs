namespace Battleship.Core;

/// <summary>Source of coordinates typed or clicked by a human.</summary>
public interface ICoordinateInput
{
    Coordinate ReadCoordinate();
}
