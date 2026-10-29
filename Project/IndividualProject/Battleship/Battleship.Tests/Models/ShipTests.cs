namespace Battleship.Tests;

using Battleship.Core;

public class ShipTests
{
    [TestCase(ShipType.Carrier, 5)]
    [TestCase(ShipType.Battleship, 4)]
    [TestCase(ShipType.Cruiser, 3)]
    [TestCase(ShipType.Submarine, 3)]
    [TestCase(ShipType.Destroyer, 2)]
    public void Constructor_ShipType_SetsLengthFromType(ShipType type, int expectedLength)
    {
        var ship = new Ship(type);

        Assert.That(ship.Type, Is.EqualTo(type));
        Assert.That(ship.Length, Is.EqualTo(expectedLength));
    }

    [Test]
    public void Constructor_UnknownShipType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Ship((ShipType)99));
    }

    [Test]
    public void Constructor_NewShip_HasNoPositionsAndNoHits()
    {
        var ship = new Ship(ShipType.Cruiser);

        Assert.That(ship.Positions, Is.Empty);
        Assert.That(ship.HitCount, Is.EqualTo(0));
    }

    [Test]
    public void IsSunk_NoHits_ReturnsFalse()
    {
        var ship = new Ship(ShipType.Destroyer);

        Assert.That(ship.IsSunk, Is.False);
    }

    [Test]
    public void IsSunk_HitCountBelowLength_ReturnsFalse()
    {
        var ship = new Ship(ShipType.Destroyer) { HitCount = 1 };

        Assert.That(ship.IsSunk, Is.False);
    }

    [Test]
    public void IsSunk_HitCountEqualsLength_ReturnsTrue()
    {
        var ship = new Ship(ShipType.Destroyer) { HitCount = 2 };

        Assert.That(ship.IsSunk, Is.True);
    }

    [Test]
    public void IsSunk_HitCountExceedsLength_ReturnsTrue()
    {
        var ship = new Ship(ShipType.Destroyer) { HitCount = 3 };

        Assert.That(ship.IsSunk, Is.True);
    }
}
