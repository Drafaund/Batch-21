namespace Battleship.Tests;

using Battleship.Core;

public class HumanPlayerTests
{
    private static HumanPlayer NewPlayer(ICoordinateInput input) => new(1, "Ann", new Board(), new Fleet(), input);

    [Test]
    public void Constructor_Arguments_KeepsIdentityBoardAndFleet()
    {
        var board = new Board();
        var fleet = new Fleet();

        var player = new HumanPlayer(7, "Ann", board, fleet, new FakeCoordinateInput());

        Assert.That(player.Id, Is.EqualTo(7));
        Assert.That(player.Name, Is.EqualTo("Ann"));
        Assert.That(player.Board, Is.SameAs(board));
        Assert.That(player.Fleet, Is.SameAs(fleet));
    }

    [Test]
    public void Constructor_NullInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HumanPlayer(1, "Ann", new Board(), new Fleet(), null!));
    }

    [Test]
    public void HumanPlayer_IsPlayerAndShotStrategy()
    {
        var player = NewPlayer(new FakeCoordinateInput());

        Assert.That(player, Is.AssignableTo<IPlayer>());
        Assert.That(player, Is.AssignableTo<IShotStrategy>());
    }

    [Test]
    public void SelectTarget_Called_ReturnsCoordinateFromInput()
    {
        var input = new FakeCoordinateInput(new Coordinate(3, 3));
        var player = NewPlayer(input);

        var target = player.SelectTarget();

        Assert.That(target, Is.EqualTo(new Coordinate(3, 3)));
        Assert.That(input.ReadCount, Is.EqualTo(1));
    }

    [Test]
    public void SelectTarget_CalledTwice_ReadsInputEachTime()
    {
        var input = new FakeCoordinateInput(new Coordinate(0, 0), new Coordinate(9, 9));
        var player = NewPlayer(input);

        Assert.That(player.SelectTarget(), Is.EqualTo(new Coordinate(0, 0)));
        Assert.That(player.SelectTarget(), Is.EqualTo(new Coordinate(9, 9)));
        Assert.That(input.ReadCount, Is.EqualTo(2));
    }

    [Test]
    public void SelectTarget_OutOfBoardInput_ReturnedUnchangedForControllerToReject()
    {
        var player = NewPlayer(new FakeCoordinateInput(new Coordinate(12, -3)));

        Assert.That(player.SelectTarget(), Is.EqualTo(new Coordinate(12, -3)));
    }

    [Test]
    public void ReceiveResult_AnyResult_DoesNotThrowAndDoesNotReadInput()
    {
        var input = new FakeCoordinateInput();
        var player = NewPlayer(input);

        foreach (var result in Enum.GetValues<ShotResult>())
        {
            player.ReceiveResult(new Coordinate(1, 1), result);
        }

        Assert.That(input.ReadCount, Is.EqualTo(0));
    }

    [Test]
    public void HumanPlayer_InGame_ShotComesFromInputAndIsResolvedByController()
    {
        var human = new HumanPlayer(1, "Ann", new Board(), new Fleet(), new FakeCoordinateInput(new Coordinate(0, 0)));
        var other = new FakePlayer(2, "Two");
        var controller = new GameController(human, human, other, new FakeShotStrategy());
        foreach (var type in TestGame.AllTypes)
        {
            controller.PlaceShip(human, new Ship(type), new Coordinate(0, TestGame.RowOf(type)), Orientation.Horizontal);
            controller.PlaceShip(other, new Ship(type), new Coordinate(0, TestGame.RowOf(type)), Orientation.Horizontal);
        }

        controller.StartGame();

        Assert.That(controller.TakeTurn(), Is.EqualTo(ShotResult.Hit));
        Assert.That(other.Board.Cells[0, 0].State, Is.EqualTo(CellState.Hit));
    }
}
