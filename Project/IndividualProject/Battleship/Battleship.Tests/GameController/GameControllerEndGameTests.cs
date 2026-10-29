namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerEndGameTests
{
    private static void SinkEverything(TestGame game, IPlayer shooter)
    {
        foreach (var cell in TestGame.AllTypes.SelectMany(TestGame.CellsOf))
        {
            game.Controller.ProcessShot(shooter, cell);
        }
    }

    [Test]
    public void CheckWinner_DuringSetup_ReturnsNull()
    {
        var game = new TestGame();

        Assert.That(game.Controller.CheckWinner(), Is.Null);
    }

    [Test]
    public void CheckWinner_GameJustStarted_ReturnsNull()
    {
        var game = new TestGame().Started();

        Assert.That(game.Controller.CheckWinner(), Is.Null);
    }

    [Test]
    public void CheckWinner_SomeShipsStillAfloat_ReturnsNull()
    {
        var game = new TestGame().Started();
        foreach (var cell in TestGame.CellsOf(ShipType.Carrier))
        {
            game.Controller.ProcessShot(game.Player1, cell);
        }

        Assert.That(game.Controller.CheckWinner(), Is.Null);
    }

    [Test]
    public void CheckWinner_AllPlayer2ShipsSunk_ReturnsPlayer1()
    {
        var game = new TestGame().Started();

        SinkEverything(game, game.Player1);

        Assert.That(game.Controller.CheckWinner(), Is.SameAs(game.Player1));
    }

    [Test]
    public void CheckWinner_AllPlayer1ShipsSunk_ReturnsPlayer2()
    {
        var game = new TestGame().Started();

        SinkEverything(game, game.Player2);

        Assert.That(game.Controller.CheckWinner(), Is.SameAs(game.Player2));
    }

    [Test]
    public void EndGame_InProgress_StateBecomesFinished()
    {
        var game = new TestGame().Started();

        game.Controller.EndGame(game.Player2);

        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.Finished));
    }

    [Test]
    public void EndGame_InProgress_RaisesOnGameEndedWithWinner()
    {
        var game = new TestGame().Started();
        var winners = new List<IPlayer>();
        game.Controller.OnGameEnded += winners.Add;

        game.Controller.EndGame(game.Player2);

        Assert.That(winners, Has.Exactly(1).Items);
        Assert.That(winners[0], Is.SameAs(game.Player2));
    }

    [Test]
    public void EndGame_GameNotStarted_Throws()
    {
        var game = new TestGame();

        Assert.Throws<InvalidOperationException>(() => game.Controller.EndGame(game.Player1));
    }

    [Test]
    public void EndGame_CalledTwice_ThrowsAndRaisesEventOnce()
    {
        var game = new TestGame().Started();
        var count = 0;
        game.Controller.OnGameEnded += _ => count++;
        game.Controller.EndGame(game.Player1);

        Assert.Throws<InvalidOperationException>(() => game.Controller.EndGame(game.Player1));
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void EndGame_WinnerNotInGame_Throws()
    {
        var game = new TestGame().Started();

        Assert.Throws<ArgumentException>(() => game.Controller.EndGame(new FakePlayer(3, "Stranger")));
    }

    [Test]
    public void EndGame_NullWinner_ThrowsAndStaysInProgress()
    {
        var game = new TestGame().Started();
        var count = 0;
        game.Controller.OnGameEnded += _ => count++;

        Assert.Throws<ArgumentNullException>(() => game.Controller.EndGame(null!));
        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.InProgress));
        Assert.That(count, Is.EqualTo(0));
    }
}
