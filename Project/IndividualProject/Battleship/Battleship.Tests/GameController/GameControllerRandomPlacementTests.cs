namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerRandomPlacementTests
{
    private static List<IShip> NewShips() => TestGame.AllTypes.Select(type => (IShip)new Ship(type)).ToList();

    [Test]
    public void PlaceShipsRandomly_FiveShips_FleetIsCompleteAndGameCanStart()
    {
        var game = new TestGame();

        game.Controller.PlaceShipsRandomly(game.Player1, NewShips(), new Random(42));
        game.Controller.PlaceShipsRandomly(game.Player2, NewShips(), new Random(43));

        Assert.That(game.Player1.Fleet.Ships.Count, Is.EqualTo(5));
        Assert.That(game.Player1.Fleet.Ships.Select(s => s.Type).OrderBy(t => t), Is.EqualTo(TestGame.AllTypes.OrderBy(t => t)));
        game.Controller.StartGame();
        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.InProgress));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(123)]
    [TestCase(2026)]
    public void PlaceShipsRandomly_AnySeed_ShipsAreInsideBoardStraightAndDoNotOverlap(int seed)
    {
        var game = new TestGame();

        game.Controller.PlaceShipsRandomly(game.Player1, NewShips(), new Random(seed));

        var all = game.Player1.Fleet.Ships.SelectMany(ship => ship.Positions).ToList();
        Assert.That(all.Count, Is.EqualTo(17));
        Assert.That(all.Distinct().Count(), Is.EqualTo(17));
        Assert.That(all.Select(c => c.X), Is.All.InRange(0, 9));
        Assert.That(all.Select(c => c.Y), Is.All.InRange(0, 9));
        foreach (var ship in game.Player1.Fleet.Ships)
        {
            Assert.That(ship.Positions.Count, Is.EqualTo(ship.Length));
            var sameRow = ship.Positions.All(p => p.Y == ship.Positions[0].Y);
            var sameColumn = ship.Positions.All(p => p.X == ship.Positions[0].X);
            Assert.That(sameRow && !sameColumn, Is.EqualTo(ship.Orientation == Orientation.Horizontal));
            Assert.That(sameColumn && !sameRow, Is.EqualTo(ship.Orientation == Orientation.Vertical));
        }
    }

    [Test]
    public void PlaceShipsRandomly_AnySeed_MarksExactlySeventeenCells()
    {
        var game = new TestGame();

        game.Controller.PlaceShipsRandomly(game.Player1, NewShips(), new Random(7));

        var occupied = game.Player1.Board.Cells.Cast<ICell>().Count(c => c.IsOccupied && c.State == CellState.Ship);
        Assert.That(occupied, Is.EqualTo(17));
    }

    [Test]
    public void PlaceShipsRandomly_SameSeed_GivesSamePlacement()
    {
        var first = new TestGame();
        var second = new TestGame();

        first.Controller.PlaceShipsRandomly(first.Player1, NewShips(), new Random(99));
        second.Controller.PlaceShipsRandomly(second.Player1, NewShips(), new Random(99));

        Assert.That(Describe(second.Player1), Is.EqualTo(Describe(first.Player1)));
    }

    [Test]
    public void PlaceShipsRandomly_DifferentSeeds_GiveDifferentPlacements()
    {
        var first = new TestGame();
        var second = new TestGame();

        first.Controller.PlaceShipsRandomly(first.Player1, NewShips(), new Random(1));
        second.Controller.PlaceShipsRandomly(second.Player1, NewShips(), new Random(2));

        Assert.That(Describe(second.Player1), Is.Not.EqualTo(Describe(first.Player1)));
    }

    [Test]
    public void PlaceShipsRandomly_ShipTypeAlreadyInFleet_Throws()
    {
        var game = new TestGame();
        game.Controller.PlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(0, 0), Orientation.Horizontal);

        Assert.Throws<InvalidOperationException>(() =>
            game.Controller.PlaceShipsRandomly(game.Player1, new IShip[] { new Ship(ShipType.Carrier) }, new Random(1)));
    }

    [Test]
    public void PlaceShipsRandomly_NullArguments_Throw()
    {
        var game = new TestGame();

        Assert.Throws<ArgumentNullException>(() => game.Controller.PlaceShipsRandomly(game.Player1, null!, new Random(1)));
        Assert.Throws<ArgumentNullException>(() => game.Controller.PlaceShipsRandomly(game.Player1, NewShips(), null!));
    }

    private static string Describe(IPlayer player)
    {
        return string.Join(";", player.Fleet.Ships.Select(ship =>
            $"{ship.Type}:{ship.Orientation}:{string.Join(",", ship.Positions.Select(p => $"{p.X}-{p.Y}"))}"));
    }
}
