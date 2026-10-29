namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerTurnTests
{
    [Test]
    public void TakeTurn_GameNotStarted_Throws()
    {
        var game = new TestGame();

        Assert.Throws<InvalidOperationException>(() => game.Controller.TakeTurn());
    }

    [Test]
    public void TakeTurn_CurrentPlayer_AsksOnlyThatPlayersStrategy()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(9, 9)), new FakeShotStrategy()).Started();

        game.Controller.TakeTurn();

        Assert.That(game.Strategy1.SelectTargetCalls, Is.EqualTo(1));
        Assert.That(game.Strategy2.SelectTargetCalls, Is.EqualTo(0));
    }

    [Test]
    public void TakeTurn_Miss_ReturnsMissAndTellsStrategyTheResult()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(9, 9)), new FakeShotStrategy()).Started();

        var result = game.Controller.TakeTurn();

        Assert.That(result, Is.EqualTo(ShotResult.Miss));
        Assert.That(game.Strategy1.Received, Is.EqualTo(new[] { (new Coordinate(9, 9), ShotResult.Miss) }));
        Assert.That(game.Strategy2.Received, Is.Empty);
        Assert.That(game.Player2.Board.Cells[9, 9].State, Is.EqualTo(CellState.Miss));
    }

    [Test]
    public void TakeTurn_Hit_TellsStrategyHit()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(0, 0)), new FakeShotStrategy()).Started();

        var result = game.Controller.TakeTurn();

        Assert.That(result, Is.EqualTo(ShotResult.Hit));
        Assert.That(game.Strategy1.Received, Is.EqualTo(new[] { (new Coordinate(0, 0), ShotResult.Hit) }));
    }

    [Test]
    public void TakeTurn_ValidShot_PassesTurnToOtherPlayer()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(9, 9)), new FakeShotStrategy()).Started();

        game.Controller.TakeTurn();

        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player2));
    }

    [Test]
    public void TakeTurn_Hit_StillPassesTurn()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(0, 0)), new FakeShotStrategy()).Started();

        game.Controller.TakeTurn();

        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player2));
    }

    [Test]
    public void TakeTurn_InvalidShot_KeepsTurnAndTellsStrategyInvalid()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(-1, 0), new Coordinate(9, 9)), new FakeShotStrategy()).Started();

        var first = game.Controller.TakeTurn();

        Assert.That(first, Is.EqualTo(ShotResult.Invalid));
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
        Assert.That(game.Strategy1.Received, Is.EqualTo(new[] { (new Coordinate(-1, 0), ShotResult.Invalid) }));

        var second = game.Controller.TakeTurn();

        Assert.That(second, Is.EqualTo(ShotResult.Miss));
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player2));
    }

    [Test]
    public void TakeTurn_RepeatedTarget_IsInvalidAndKeepsTurn()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(9, 9), new Coordinate(9, 9)), new FakeShotStrategy(new Coordinate(9, 9))).Started();

        game.Controller.TakeTurn();
        game.Controller.TakeTurn();
        var repeated = game.Controller.TakeTurn();

        Assert.That(repeated, Is.EqualTo(ShotResult.Invalid));
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
    }

    [Test]
    public void TakeTurn_TwoTurns_UsesEachPlayersOwnStrategyInOrder()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(9, 9)), new FakeShotStrategy(new Coordinate(8, 8))).Started();

        game.Controller.TakeTurn();
        game.Controller.TakeTurn();

        Assert.That(game.Player2.Board.Cells[9, 9].State, Is.EqualTo(CellState.Miss));
        Assert.That(game.Player1.Board.Cells[8, 8].State, Is.EqualTo(CellState.Miss));
        Assert.That(game.Strategy1.SelectTargetCalls, Is.EqualTo(1));
        Assert.That(game.Strategy2.SelectTargetCalls, Is.EqualTo(1));
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
    }

    [Test]
    public void TakeTurn_Fired_RaisesOnShotResolvedBeforeStrategyReceivesResult()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(0, 0)), new FakeShotStrategy()).Started();
        var received = -1;
        game.Controller.OnShotResolved += (_, _, _) => received = game.Strategy1.Received.Count;

        game.Controller.TakeTurn();

        Assert.That(received, Is.EqualTo(0));
        Assert.That(game.Strategy1.Received, Has.Exactly(1).Items);
    }

    [Test]
    public void TakeTurn_NoWinnerYet_GameStaysInProgress()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(0, 0)), new FakeShotStrategy()).Started();
        var ended = false;
        game.Controller.OnGameEnded += _ => ended = true;

        game.Controller.TakeTurn();

        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.InProgress));
        Assert.That(ended, Is.False);
    }

    [Test]
    public void TakeTurn_SinksLastEnemyShip_EndsGameWithShooterAsWinnerAndKeepsTurn()
    {
        var player1Targets = Enum.GetValues<ShipType>().SelectMany(TestGame.CellsOf).ToArray();
        var player2Targets = Enumerable.Range(0, 10).Select(x => new Coordinate(x, 9))
            .Concat(Enumerable.Range(0, 6).Select(x => new Coordinate(x, 8)))
            .ToArray();
        var game = new TestGame(new FakeShotStrategy(player1Targets), new FakeShotStrategy(player2Targets)).Started();
        IPlayer? winner = null;
        var endedCount = 0;
        game.Controller.OnGameEnded += w =>
        {
            winner = w;
            endedCount++;
        };

        ShotResult last = ShotResult.Invalid;
        var turns = 0;
        while (game.Controller.GetState() == GameState.InProgress)
        {
            last = game.Controller.TakeTurn();
            turns++;
        }

        Assert.That(turns, Is.EqualTo(34 - 1));
        Assert.That(last, Is.EqualTo(ShotResult.Sunk));
        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.Finished));
        Assert.That(winner, Is.SameAs(game.Player1));
        Assert.That(endedCount, Is.EqualTo(1));
        Assert.That(game.Controller.GetRemainingShips(game.Player2), Is.EqualTo(0));
        Assert.That(game.Controller.GetRemainingShips(game.Player1), Is.EqualTo(5));
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
    }

    [Test]
    public void TakeTurn_Player2SinksLastEnemyShip_EndsGameWithPlayer2AsWinnerAndKeepsTurn()
    {
        var player1Misses = Enumerable.Range(0, 10).Select(x => new Coordinate(x, 9))
            .Concat(Enumerable.Range(0, 7).Select(x => new Coordinate(x, 8)))
            .ToArray();
        var player2Targets = Enum.GetValues<ShipType>().SelectMany(TestGame.CellsOf).ToArray();
        var game = new TestGame(new FakeShotStrategy(player1Misses), new FakeShotStrategy(player2Targets)).Started();
        IPlayer? winner = null;
        var endedCount = 0;
        game.Controller.OnGameEnded += w =>
        {
            winner = w;
            endedCount++;
        };

        ShotResult last = ShotResult.Invalid;
        var turns = 0;
        while (game.Controller.GetState() == GameState.InProgress)
        {
            last = game.Controller.TakeTurn();
            turns++;
        }

        Assert.That(turns, Is.EqualTo(34));
        Assert.That(last, Is.EqualTo(ShotResult.Sunk));
        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.Finished));
        Assert.That(winner, Is.SameAs(game.Player2));
        Assert.That(endedCount, Is.EqualTo(1));
        Assert.That(game.Controller.GetRemainingShips(game.Player1), Is.EqualTo(0));
        Assert.That(game.Controller.GetRemainingShips(game.Player2), Is.EqualTo(5));
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player2));
    }

    [Test]
    public void TakeTurn_AfterGameEnded_Throws()
    {
        var game = new TestGame().Started();
        game.Controller.EndGame(game.Player1);

        Assert.Throws<InvalidOperationException>(() => game.Controller.TakeTurn());
    }
}
