namespace Battleship.Tests;

using Battleship.Core;

public class ShipTests
{
    [Theory]
    [InlineData(ShipType.Carrier, 5)]
    [InlineData(ShipType.Battleship, 4)]
    [InlineData(ShipType.Cruiser, 3)]
    [InlineData(ShipType.Submarine, 3)]
    [InlineData(ShipType.Destroyer, 2)]
    public void Constructor_ShipType_SetsLengthFromType(ShipType type, int expectedLength)
    {
        var ship = new Ship(type);

        Assert.Equal(type, ship.Type);
        Assert.Equal(expectedLength, ship.Length);
    }

    [Fact]
    public void Constructor_UnknownShipType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Ship((ShipType)99));
    }

    [Fact]
    public void Constructor_NewShip_HasNoPositionsAndNoHits()
    {
        var ship = new Ship(ShipType.Cruiser);

        Assert.Empty(ship.Positions);
        Assert.Equal(0, ship.HitCount);
    }

    [Fact]
    public void IsSunk_NoHits_ReturnsFalse()
    {
        var ship = new Ship(ShipType.Destroyer);

        Assert.False(ship.IsSunk);
    }

    [Fact]
    public void IsSunk_HitCountBelowLength_ReturnsFalse()
    {
        var ship = new Ship(ShipType.Destroyer) { HitCount = 1 };

        Assert.False(ship.IsSunk);
    }

    [Fact]
    public void IsSunk_HitCountEqualsLength_ReturnsTrue()
    {
        var ship = new Ship(ShipType.Destroyer) { HitCount = 2 };

        Assert.True(ship.IsSunk);
    }

    [Fact]
    public void IsSunk_HitCountExceedsLength_ReturnsTrue()
    {
        var ship = new Ship(ShipType.Destroyer) { HitCount = 3 };

        Assert.True(ship.IsSunk);
    }
}
