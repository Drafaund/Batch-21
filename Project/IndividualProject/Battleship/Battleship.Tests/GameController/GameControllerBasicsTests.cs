namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerBasicsTests
{
    [Test]
    public void Constructor_NewGame_StateIsSetup()
    {
        var game = new TestGame();

        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.Setup));
        Assert.That(game.Controller.State, Is.EqualTo(GameState.Setup));
    }

    [Test]
    public void Constructor_NewGame_FirstPlayerIsCurrent()
    {
        var game = new TestGame();

        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
    }

    [Test]
    public void Constructor_NewGame_ExposesBothPlayers()
    {
        var game = new TestGame();

        Assert.That(game.Controller.Player1, Is.SameAs(game.Player1));
        Assert.That(game.Controller.Player2, Is.SameAs(game.Player2));
    }

    [Test]
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

    [Test]
    public void Constructor_SamePlayerTwice_Throws()
    {
        var p = new FakePlayer(1, "One");
        var s = new FakeShotStrategy();

        Assert.Throws<ArgumentException>(() => new GameController(p, s, p, s));
    }

    [Test]
    public void GetBoard_EachPlayer_ReturnsThatPlayersBoard()
    {
        var game = new TestGame();

        Assert.That(game.Controller.GetBoard(game.Player1), Is.SameAs(game.Player1.Board));
        Assert.That(game.Controller.GetBoard(game.Player2), Is.SameAs(game.Player2.Board));
    }

    [Test]
    public void GetFleet_EachPlayer_ReturnsThatPlayersFleet()
    {
        var game = new TestGame();

        Assert.That(game.Controller.GetFleet(game.Player1), Is.SameAs(game.Player1.Fleet));
        Assert.That(game.Controller.GetFleet(game.Player2), Is.SameAs(game.Player2.Fleet));
    }

    [Test]
    public void BoardAndFleetDictionaries_AfterConstruction_HoldEachPlayersBoardAndFleet()
    {
        var game = new TestGame();

        Assert.That(game.Controller.Board.Count, Is.EqualTo(2));
        Assert.That(game.Controller.Fleet.Count, Is.EqualTo(2));
        Assert.That(game.Controller.Board[game.Player1], Is.SameAs(game.Player1.Board));
        Assert.That(game.Controller.Board[game.Player2], Is.SameAs(game.Player2.Board));
        Assert.That(game.Controller.Fleet[game.Player1], Is.SameAs(game.Player1.Fleet));
        Assert.That(game.Controller.Fleet[game.Player2], Is.SameAs(game.Player2.Fleet));
    }

    [Test]
    public void GetBoardAndGetFleet_PlayerNotInGame_Throw()
    {
        var game = new TestGame();
        var stranger = new FakePlayer(3, "Stranger");

        Assert.Throws<ArgumentException>(() => game.Controller.GetBoard(stranger));
        Assert.Throws<ArgumentException>(() => game.Controller.GetFleet(stranger));
    }

    [Test]
    public void SwitchTurn_Called_AlternatesCurrentPlayer()
    {
        var game = new TestGame();

        game.Controller.SwitchTurn();
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player2));

        game.Controller.SwitchTurn();
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
    }
}
