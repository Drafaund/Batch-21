namespace Battleship.Tests;

using Battleship.Core;

public class FleetTests
{
    [Fact]
    public void Constructor_NewFleet_ShipsIsEmptyAndNotNull()
    {
        var fleet = new Fleet();

        Assert.NotNull(fleet.Ships);
        Assert.Empty(fleet.Ships);
    }

    [Fact]
    public void Ships_AddShip_ContainsShip()
    {
        var fleet = new Fleet();
        var ship = new Ship(ShipType.Carrier);

        fleet.Ships.Add(ship);

        Assert.Single(fleet.Ships);
        Assert.Same(ship, fleet.Ships[0]);
    }

    [Fact]
    public void Constructor_TwoFleets_DoNotShareShipLists()
    {
        var first = new Fleet();
        var second = new Fleet();

        first.Ships.Add(new Ship(ShipType.Cruiser));

        Assert.Empty(second.Ships);
    }
}
