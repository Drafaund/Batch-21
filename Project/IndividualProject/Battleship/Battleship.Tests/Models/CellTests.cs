namespace Battleship.Tests;

using Battleship.Core;

public class CellTests
{
    [Fact]
    public void Constructor_Position_KeepsPosition()
    {
        var position = new Coordinate(3, 7);

        var cell = new Cell(position);

        Assert.Equal(position, cell.Position);
    }

    [Fact]
    public void Constructor_NewCell_IsEmptyAndHasNoShip()
    {
        var cell = new Cell(new Coordinate(0, 0));

        Assert.Equal(CellState.Empty, cell.State);
        Assert.Null(cell.OccupyingShip);
    }

    [Fact]
    public void IsOccupied_NoShip_ReturnsFalse()
    {
        var cell = new Cell(new Coordinate(0, 0));

        Assert.False(cell.IsOccupied);
    }

    [Fact]
    public void IsOccupied_WithShip_ReturnsTrue()
    {
        var cell = new Cell(new Coordinate(0, 0)) { OccupyingShip = new Ship(ShipType.Destroyer) };

        Assert.True(cell.IsOccupied);
    }

    [Fact]
    public void IsOccupied_ShipRemoved_ReturnsFalse()
    {
        var cell = new Cell(new Coordinate(0, 0)) { OccupyingShip = new Ship(ShipType.Destroyer) };

        cell.OccupyingShip = null;

        Assert.False(cell.IsOccupied);
    }
}
