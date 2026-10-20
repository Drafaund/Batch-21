namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerRandomPlacementTests
{
    private static List<IShip> NewShips() => TestGame.AllTypes.Select(type => (IShip)new Ship(type)).ToList();

    [Fact]
    public void PlaceShipsRandomly_FiveShips_FleetIsCompleteAndGameCanStart()
    {
        var game = new TestGame();

        game.Controller.PlaceShipsRandomly(game.Player1, NewShips(), new Random(42));
        game.Controller.PlaceShipsRandomly(game.Player2, NewShips(), new Random(43));

        Assert.Equal(5, game.Player1.Fleet.Ships.Count);
        Assert.Equal(TestGame.AllTypes.OrderBy(t => t), game.Player1.Fleet.Ships.Select(s => s.Type).OrderBy(t => t));
        game.Controller.StartGame();
        Assert.Equal(GameState.InProgress, game.Controller.GetState());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(123)]
    [InlineData(2026)]
    public void PlaceShipsRandomly_AnySeed_ShipsAreInsideBoardStraightAndDoNotOverlap(int seed)
    {
        var game = new TestGame();

        game.Controller.PlaceShipsRandomly(game.Player1, NewShips(), new Random(seed));

        var all = game.Player1.Fleet.Ships.SelectMany(ship => ship.Positions).ToList();
        Assert.Equal(17, all.Count);
        Assert.Equal(17, all.Distinct().Count());
        Assert.All(all, c => Assert.InRange(c.X, 0, 9));
        Assert.All(all, c => Assert.InRange(c.Y, 0, 9));
        foreach (var ship in game.Player1.Fleet.Ships)
        {
            Assert.Equal(ship.Length, ship.Positions.Count);
            var sameRow = ship.Positions.All(p => p.Y == ship.Positions[0].Y);
            var sameColumn = ship.Positions.All(p => p.X == ship.Positions[0].X);
            Assert.Equal(ship.Orientation == Orientation.Horizontal, sameRow && !sameColumn);
            Assert.Equal(ship.Orientation == Orientation.Vertical, sameColumn && !sameRow);
        }
    }

    [Fact]
    public void PlaceShipsRandomly_AnySeed_MarksExactlySeventeenCells()
    {
        var game = new TestGame();

        game.Controller.PlaceShipsRandomly(game.Player1, NewShips(), new Random(7));

        var occupied = game.Player1.Board.Cells.Cast<ICell>().Count(c => c.IsOccupied && c.State == CellState.Ship);
        Assert.Equal(17, occupied);
    }

    [Fact]
    public void PlaceShipsRandomly_SameSeed_GivesSamePlacement()
    {
        var first = new TestGame();
        var second = new TestGame();

        first.Controller.PlaceShipsRandomly(first.Player1, NewShips(), new Random(99));
        second.Controller.PlaceShipsRandomly(second.Player1, NewShips(), new Random(99));

        Assert.Equal(Describe(first.Player1), Describe(second.Player1));
    }

    [Fact]
    public void PlaceShipsRandomly_DifferentSeeds_GiveDifferentPlacements()
    {
        var first = new TestGame();
        var second = new TestGame();

        first.Controller.PlaceShipsRandomly(first.Player1, NewShips(), new Random(1));
        second.Controller.PlaceShipsRandomly(second.Player1, NewShips(), new Random(2));

        Assert.NotEqual(Describe(first.Player1), Describe(second.Player1));
    }

    [Fact]
    public void PlaceShipsRandomly_ShipTypeAlreadyInFleet_Throws()
    {
        var game = new TestGame();
        game.Controller.PlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(0, 0), Orientation.Horizontal);

        Assert.Throws<InvalidOperationException>(() =>
            game.Controller.PlaceShipsRandomly(game.Player1, new IShip[] { new Ship(ShipType.Carrier) }, new Random(1)));
    }

    [Fact]
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
