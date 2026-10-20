namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerBasicsTests
{
    [Fact]
    public void Constructor_NewGame_StateIsSetup()
    {
        var game = new TestGame();

        Assert.Equal(GameState.Setup, game.Controller.GetState());
        Assert.Equal(GameState.Setup, game.Controller.State);
    }

    [Fact]
    public void Constructor_NewGame_FirstPlayerIsCurrent()
    {
        var game = new TestGame();

        Assert.Same(game.Player1, game.Controller.GetCurrentPlayer());
    }

    [Fact]
    public void Constructor_NewGame_ExposesBothPlayers()
    {
        var game = new TestGame();

        Assert.Same(game.Player1, game.Controller.Player1);
        Assert.Same(game.Player2, game.Controller.Player2);
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        var p1 = new FakePlayer(1, "One");
        var p2 = new FakePlayer(2, "Two");
        var s = new FakeShotStrategy();

        Assert.Throws<ArgumentNullException>(() => new GameController(null!, s, p2, s));
        Assert.Throws<ArgumentNullException>(() => new GameController(p1, null!, p2, s));
        Assert.Throws<ArgumentNullException>(() => new GameController(p1, s, null!, s));
        Assert.Throws<ArgumentNullException>(() => new GameController(p1, s, p2, null!));
    }

    [Fact]
    public void Constructor_SamePlayerTwice_Throws()
    {
        var p = new FakePlayer(1, "One");
        var s = new FakeShotStrategy();

        Assert.Throws<ArgumentException>(() => new GameController(p, s, p, s));
    }

    [Fact]
    public void GetBoard_EachPlayer_ReturnsThatPlayersBoard()
    {
        var game = new TestGame();

        Assert.Same(game.Player1.Board, game.Controller.GetBoard(game.Player1));
        Assert.Same(game.Player2.Board, game.Controller.GetBoard(game.Player2));
    }

    [Fact]
    public void GetFleet_EachPlayer_ReturnsThatPlayersFleet()
    {
        var game = new TestGame();

        Assert.Same(game.Player1.Fleet, game.Controller.GetFleet(game.Player1));
        Assert.Same(game.Player2.Fleet, game.Controller.GetFleet(game.Player2));
    }

    [Fact]
    public void GetBoardAndGetFleet_PlayerNotInGame_Throw()
    {
        var game = new TestGame();
        var stranger = new FakePlayer(3, "Stranger");

        Assert.Throws<ArgumentException>(() => game.Controller.GetBoard(stranger));
        Assert.Throws<ArgumentException>(() => game.Controller.GetFleet(stranger));
    }

    [Fact]
    public void SwitchTurn_Called_AlternatesCurrentPlayer()
    {
        var game = new TestGame();

        game.Controller.SwitchTurn();
        Assert.Same(game.Player2, game.Controller.GetCurrentPlayer());

        game.Controller.SwitchTurn();
        Assert.Same(game.Player1, game.Controller.GetCurrentPlayer());
    }
}
