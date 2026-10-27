namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerTurnTests
{
    [Fact]
    public void TakeTurn_GameNotStarted_Throws()
    {
        var game = new TestGame();

        Assert.Throws<InvalidOperationException>(() => game.Controller.TakeTurn());
    }

    [Fact]
    public void TakeTurn_CurrentPlayer_AsksOnlyThatPlayersStrategy()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(9, 9)), new FakeShotStrategy()).Started();

        game.Controller.TakeTurn();

        Assert.Equal(1, game.Strategy1.SelectTargetCalls);
        Assert.Equal(0, game.Strategy2.SelectTargetCalls);
    }

    [Fact]
    public void TakeTurn_Miss_ReturnsMissAndTellsStrategyTheResult()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(9, 9)), new FakeShotStrategy()).Started();

        var result = game.Controller.TakeTurn();

        Assert.Equal(ShotResult.Miss, result);
        Assert.Equal(new[] { (new Coordinate(9, 9), ShotResult.Miss) }, game.Strategy1.Received);
        Assert.Empty(game.Strategy2.Received);
        Assert.Equal(CellState.Miss, game.Player2.Board.Cells[9, 9].State);
    }

    [Fact]
    public void TakeTurn_Hit_TellsStrategyHit()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(0, 0)), new FakeShotStrategy()).Started();

        var result = game.Controller.TakeTurn();

        Assert.Equal(ShotResult.Hit, result);
        Assert.Equal(new[] { (new Coordinate(0, 0), ShotResult.Hit) }, game.Strategy1.Received);
    }

    [Fact]
    public void TakeTurn_ValidShot_PassesTurnToOtherPlayer()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(9, 9)), new FakeShotStrategy()).Started();

        game.Controller.TakeTurn();

        Assert.Same(game.Player2, game.Controller.GetCurrentPlayer());
    }

    [Fact]
    public void TakeTurn_HitStillPassesTurn()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(0, 0)), new FakeShotStrategy()).Started();

        game.Controller.TakeTurn();

        Assert.Same(game.Player2, game.Controller.GetCurrentPlayer());
    }

    [Fact]
    public void TakeTurn_InvalidShot_KeepsTurnAndTellsStrategyInvalid()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(-1, 0), new Coordinate(9, 9)), new FakeShotStrategy()).Started();

        var first = game.Controller.TakeTurn();

        Assert.Equal(ShotResult.Invalid, first);
        Assert.Same(game.Player1, game.Controller.GetCurrentPlayer());
        Assert.Equal(new[] { (new Coordinate(-1, 0), ShotResult.Invalid) }, game.Strategy1.Received);

        var second = game.Controller.TakeTurn();

        Assert.Equal(ShotResult.Miss, second);
        Assert.Same(game.Player2, game.Controller.GetCurrentPlayer());
    }

    [Fact]
    public void TakeTurn_RepeatedTarget_IsInvalidAndKeepsTurn()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(9, 9), new Coordinate(9, 9)), new FakeShotStrategy(new Coordinate(9, 9))).Started();

        game.Controller.TakeTurn();
        game.Controller.TakeTurn();
        var repeated = game.Controller.TakeTurn();

        Assert.Equal(ShotResult.Invalid, repeated);
        Assert.Same(game.Player1, game.Controller.GetCurrentPlayer());
    }

    [Fact]
    public void TakeTurn_TwoTurns_UsesEachPlayersOwnStrategyInOrder()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(9, 9)), new FakeShotStrategy(new Coordinate(8, 8))).Started();

        game.Controller.TakeTurn();
        game.Controller.TakeTurn();

        Assert.Equal(CellState.Miss, game.Player2.Board.Cells[9, 9].State);
        Assert.Equal(CellState.Miss, game.Player1.Board.Cells[8, 8].State);
        Assert.Equal(1, game.Strategy1.SelectTargetCalls);
        Assert.Equal(1, game.Strategy2.SelectTargetCalls);
        Assert.Same(game.Player1, game.Controller.GetCurrentPlayer());
    }

    [Fact]
    public void TakeTurn_Fired_RaisesOnShotResolvedBeforeStrategyReceivesResult()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(0, 0)), new FakeShotStrategy()).Started();
        var received = -1;
        game.Controller.OnShotResolved += (_, _, _) => received = game.Strategy1.Received.Count;

        game.Controller.TakeTurn();

        Assert.Equal(0, received);
        Assert.Single(game.Strategy1.Received);
    }

    [Fact]
    public void TakeTurn_NoWinnerYet_GameStaysInProgress()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(0, 0)), new FakeShotStrategy()).Started();
        var ended = false;
        game.Controller.OnGameEnded += _ => ended = true;

        game.Controller.TakeTurn();

        Assert.Equal(GameState.InProgress, game.Controller.GetState());
        Assert.False(ended);
    }

    [Fact]
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

        Assert.Equal(34 - 1, turns);
        Assert.Equal(ShotResult.Sunk, last);
        Assert.Equal(GameState.Finished, game.Controller.GetState());
        Assert.Same(game.Player1, winner);
        Assert.Equal(1, endedCount);
        Assert.Equal(0, game.Controller.GetRemainingShips(game.Player2));
        Assert.Equal(5, game.Controller.GetRemainingShips(game.Player1));
        Assert.Same(game.Player1, game.Controller.GetCurrentPlayer());
    }

    [Fact]
    public void TakeTurn_AfterGameEnded_Throws()
    {
        var game = new TestGame().Started();
        game.Controller.EndGame(game.Player1);

        Assert.Throws<InvalidOperationException>(() => game.Controller.TakeTurn());
    }
}
