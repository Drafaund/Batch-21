namespace Battleship.Tests;

using Battleship.Core;

public class TestGame
{
    public static readonly ShipType[] AllTypes = Enum.GetValues<ShipType>();

    public TestGame(FakeShotStrategy? strategy1 = null, FakeShotStrategy? strategy2 = null)
    {
        Player1 = new FakePlayer(1, "One");
        Player2 = new FakePlayer(2, "Two");
        Strategy1 = strategy1 ?? new FakeShotStrategy();
        Strategy2 = strategy2 ?? new FakeShotStrategy();
        Controller = new GameController(Player1, Strategy1, Player2, Strategy2);
    }

    public FakePlayer Player1 { get; }
    public FakePlayer Player2 { get; }
    public FakeShotStrategy Strategy1 { get; }
    public FakeShotStrategy Strategy2 { get; }
    public GameController Controller { get; }

    public static int RowOf(ShipType type) => (int)type;

    public static IEnumerable<Coordinate> CellsOf(ShipType type)
    {
        var length = new Ship(type).Length;
        return Enumerable.Range(0, length).Select(x => new Coordinate(x, RowOf(type)));
    }

    public void PlaceFleet(IPlayer player)
    {
        foreach (var type in AllTypes)
        {
            var placed = Controller.PlaceShip(player, new Ship(type), new Coordinate(0, RowOf(type)), Orientation.Horizontal);
            Assert.True(placed, $"Fixture failed to place {type}.");
        }
    }

    public TestGame Started()
    {
        PlaceFleet(Player1);
        PlaceFleet(Player2);
        Controller.StartGame();
        return this;
    }
}
