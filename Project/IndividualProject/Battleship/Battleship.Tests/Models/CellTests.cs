namespace Battleship.Tests;

using Battleship.Core;

public class CellTests
{
    [Test]
    public void Constructor_Position_KeepsPosition()
    {
        var position = new Coordinate(3, 7);

        var cell = new Cell(position);

        Assert.That(cell.Position, Is.EqualTo(position));
    }

    [Test]
    public void Constructor_NewCell_IsEmptyAndHasNoShip()
    {
        var cell = new Cell(new Coordinate(0, 0));

        Assert.That(cell.State, Is.EqualTo(CellState.Empty));
        Assert.That(cell.OccupyingShip, Is.Null);
    }

    [Test]
    public void IsOccupied_NoShip_ReturnsFalse()
    {
        var cell = new Cell(new Coordinate(0, 0));

        Assert.That(cell.IsOccupied, Is.False);
    }

    [Test]
    public void IsOccupied_WithShip_ReturnsTrue()
    {
        var cell = new Cell(new Coordinate(0, 0)) { OccupyingShip = new Ship(ShipType.Destroyer) };

        Assert.That(cell.IsOccupied, Is.True);
    }

    [Test]
    public void IsOccupied_ShipRemoved_ReturnsFalse()
    {
        var cell = new Cell(new Coordinate(0, 0)) { OccupyingShip = new Ship(ShipType.Destroyer) };

        cell.OccupyingShip = null;

        Assert.That(cell.IsOccupied, Is.False);
    }
}
