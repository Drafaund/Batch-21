namespace Battleship.Tests;

using Battleship.Core;

public class HumanPlayerTests
{
    private static HumanPlayer NewPlayer(ICoordinateInput input) => new(1, "Ann", new Board(), new Fleet(), input);

    [Fact]
    public void Constructor_Arguments_KeepsIdentityBoardAndFleet()
    {
        var board = new Board();
        var fleet = new Fleet();

        var player = new HumanPlayer(7, "Ann", board, fleet, new FakeCoordinateInput());

        Assert.Equal(7, player.Id);
        Assert.Equal("Ann", player.Name);
        Assert.Same(board, player.Board);
        Assert.Same(fleet, player.Fleet);
    }

    [Fact]
    public void Constructor_NullInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HumanPlayer(1, "Ann", new Board(), new Fleet(), null!));
    }

    [Fact]
    public void HumanPlayer_IsPlayerAndShotStrategy()
    {
        var player = NewPlayer(new FakeCoordinateInput());

        Assert.IsAssignableFrom<IPlayer>(player);
        Assert.IsAssignableFrom<IShotStrategy>(player);
    }

    [Fact]
    public void SelectTarget_Called_ReturnsCoordinateFromInput()
    {
        var input = new FakeCoordinateInput(new Coordinate(3, 3));
        var player = NewPlayer(input);

        var target = player.SelectTarget();

        Assert.Equal(new Coordinate(3, 3), target);
        Assert.Equal(1, input.ReadCount);
    }

    [Fact]
    public void SelectTarget_CalledTwice_ReadsInputEachTime()
    {
        var input = new FakeCoordinateInput(new Coordinate(0, 0), new Coordinate(9, 9));
        var player = NewPlayer(input);

        Assert.Equal(new Coordinate(0, 0), player.SelectTarget());
        Assert.Equal(new Coordinate(9, 9), player.SelectTarget());
        Assert.Equal(2, input.ReadCount);
    }

    [Fact]
    public void SelectTarget_OutOfBoardInput_ReturnedUnchangedForControllerToReject()
    {
        var player = NewPlayer(new FakeCoordinateInput(new Coordinate(12, -3)));

        Assert.Equal(new Coordinate(12, -3), player.SelectTarget());
    }

    [Fact]
    public void ReceiveResult_AnyResult_DoesNotThrowAndDoesNotReadInput()
    {
        var input = new FakeCoordinateInput();
        var player = NewPlayer(input);

        foreach (var result in Enum.GetValues<ShotResult>())
        {
            player.ReceiveResult(new Coordinate(1, 1), result);
        }

        Assert.Equal(0, input.ReadCount);
    }

    [Fact]
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

        Assert.Equal(ShotResult.Hit, controller.TakeTurn());
        Assert.Equal(CellState.Hit, other.Board.Cells[0, 0].State);
    }
}
