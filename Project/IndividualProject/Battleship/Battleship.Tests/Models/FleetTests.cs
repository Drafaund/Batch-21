namespace Battleship.Tests;

using Battleship.Core;

public class FleetTests
{
    [Test]
    public void Constructor_NewFleet_ShipsIsEmptyAndNotNull()
    {
        var fleet = new Fleet();

        Assert.That(fleet.Ships, Is.Not.Null);
        Assert.That(fleet.Ships, Is.Empty);
    }

    [Test]
    public void Ships_AddShip_ContainsShip()
    {
        var fleet = new Fleet();
        var ship = new Ship(ShipType.Carrier);

        fleet.Ships.Add(ship);

        Assert.That(fleet.Ships, Has.Exactly(1).Items);
        Assert.That(fleet.Ships[0], Is.SameAs(ship));
    }

    [Test]
    public void Constructor_TwoFleets_DoNotShareShipLists()
    {
        var first = new Fleet();
        var second = new Fleet();

        first.Ships.Add(new Ship(ShipType.Cruiser));

        Assert.That(second.Ships, Is.Empty);
    }
}
