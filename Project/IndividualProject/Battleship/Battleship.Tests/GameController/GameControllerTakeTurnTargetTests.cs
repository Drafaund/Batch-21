namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerTakeTurnTargetTests
{
    [Fact]
    public void TakeTurnWithTarget_EmptyCell_ReturnsMissAndMarksCell()
    {
        var game = new TestGame().Started();

        var result = game.Controller.TakeTurn(new Coordinate(9, 9));

        Assert.Equal(ShotResult.Miss, result);
        Assert.Equal(CellState.Miss, game.Player2.Board.Cells[9, 9].State);
    }

    [Fact]
    public void TakeTurnWithTarget_ShipCell_ReturnsHitAndCountsHit()
    {
        var game = new TestGame().Started();

        var result = game.Controller.TakeTurn(new Coordinate(0, 0));

        var carrier = game.Player2.Fleet.Ships.Single(s => s.Type == ShipType.Carrier);
        Assert.Equal(ShotResult.Hit, result);
        Assert.Equal(1, carrier.HitCount);
    }

    [Fact]
    public void TakeTurnWithTarget_Fired_NeverAsksAStrategyForATarget()
    {
        var game = new TestGame().Started();

        game.Controller.TakeTurn(new Coordinate(9, 9));
        game.Controller.TakeTurn(new Coordinate(8, 8));

        Assert.Equal(0, game.Strategy1.SelectTargetCalls);
        Assert.Equal(0, game.Strategy2.SelectTargetCalls);
    }

    [Fact]
    public void TakeTurnWithTarget_Fired_TellsOnlyTheCurrentPlayersStrategyTheResult()
    {
        var game = new TestGame().Started();

        game.Controller.TakeTurn(new Coordinate(0, 0));

        Assert.Equal(new[] { (new Coordinate(0, 0), ShotResult.Hit) }, game.Strategy1.Received);
        Assert.Empty(game.Strategy2.Received);
    }

    [Fact]
    public void TakeTurnWithTarget_ValidShot_PassesTurnToOtherPlayer()
    {
        var game = new TestGame().Started();

        game.Controller.TakeTurn(new Coordinate(9, 9));

        Assert.Same(game.Player2, game.Controller.GetCurrentPlayer());
    }

    [Fact]
    public void TakeTurnWithTarget_SecondPlayersTurn_HitsPlayer1BoardAndTellsStrategy2()
    {
        var game = new TestGame().Started();
        game.Controller.TakeTurn(new Coordinate(9, 9));

        var result = game.Controller.TakeTurn(new Coordinate(0, 0));

        Assert.Equal(ShotResult.Hit, result);
        Assert.Equal(CellState.Hit, game.Player1.Board.Cells[0, 0].State);
        Assert.Equal(new[] { (new Coordinate(0, 0), ShotResult.Hit) }, game.Strategy2.Received);
        Assert.Same(game.Player1, game.Controller.GetCurrentPlayer());
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(10, 0)]
    [InlineData(0, 10)]
    public void TakeTurnWithTarget_OutsideBoard_ReturnsInvalidKeepsTurnAndTellsStrategy(int x, int y)
    {
        var game = new TestGame().Started();

        var result = game.Controller.TakeTurn(new Coordinate(x, y));

        Assert.Equal(ShotResult.Invalid, result);
        Assert.Same(game.Player1, game.Controller.GetCurrentPlayer());
        Assert.Equal(new[] { (new Coordinate(x, y), ShotResult.Invalid) }, game.Strategy1.Received);
    }

    [Fact]
    public void TakeTurnWithTarget_CellAlreadyFiredAt_ReturnsInvalidAndKeepsTurn()
    {
        var game = new TestGame().Started();
        game.Controller.TakeTurn(new Coordinate(9, 9)); // player 1
        game.Controller.TakeTurn(new Coordinate(9, 9)); // player 2, on the other board
        // Back to player 1, who already fired at (9,9).

        var result = game.Controller.TakeTurn(new Coordinate(9, 9));

        Assert.Equal(ShotResult.Invalid, result);
        Assert.Same(game.Player1, game.Controller.GetCurrentPlayer());
    }

    [Fact]
    public void TakeTurnWithTarget_Fired_RaisesOnShotResolvedWithTheCurrentPlayerAsShooter()
    {
        var game = new TestGame().Started();
        var calls = new List<(IPlayer Shooter, Coordinate Target, ShotResult Result)>();
        game.Controller.OnShotResolved += (shooter, target, result) => calls.Add((shooter, target, result));

        game.Controller.TakeTurn(new Coordinate(0, 0));
        game.Controller.TakeTurn(new Coordinate(9, 9));

        Assert.Equal(2, calls.Count);
        Assert.Same(game.Player1, calls[0].Shooter);
        Assert.Equal((new Coordinate(0, 0), ShotResult.Hit), (calls[0].Target, calls[0].Result));
        Assert.Same(game.Player2, calls[1].Shooter);
        Assert.Equal((new Coordinate(9, 9), ShotResult.Miss), (calls[1].Target, calls[1].Result));
    }

    [Fact]
    public void TakeTurnWithTarget_SinksLastEnemyShip_EndsGameWithShooterAsWinnerAndKeepsTurn()
    {
        var game = new TestGame().Started();
        IPlayer? winner = null;
        var endedCount = 0;
        game.Controller.OnGameEnded += w =>
        {
            winner = w;
            endedCount++;
        };

        var player1Targets = TestGame.AllTypes.SelectMany(TestGame.CellsOf).ToList();
        var player2Targets = Enumerable.Range(0, 10).Select(x => new Coordinate(x, 9))
            .Concat(Enumerable.Range(0, 6).Select(x => new Coordinate(x, 8)))
            .ToList();

        var last = ShotResult.Invalid;
        for (var i = 0; i < player1Targets.Count; i++)
        {
            last = game.Controller.TakeTurn(player1Targets[i]);
            if (game.Controller.GetState() == GameState.InProgress)
            {
                game.Controller.TakeTurn(player2Targets[i]);
            }
        }

        Assert.Equal(ShotResult.Sunk, last);
        Assert.Equal(GameState.Finished, game.Controller.GetState());
        Assert.Same(game.Player1, winner);
        Assert.Equal(1, endedCount);
        Assert.Equal(0, game.Controller.GetRemainingShips(game.Player2));
        Assert.Same(game.Player1, game.Controller.GetCurrentPlayer());
    }

    [Fact]
    public void TakeTurnWithTarget_GameNotStarted_Throws()
    {
        var game = new TestGame();

        Assert.Throws<InvalidOperationException>(() => game.Controller.TakeTurn(new Coordinate(0, 0)));
    }

    [Fact]
    public void TakeTurnWithTarget_AfterGameEnded_Throws()
    {
        var game = new TestGame().Started();
        game.Controller.EndGame(game.Player1);

        Assert.Throws<InvalidOperationException>(() => game.Controller.TakeTurn(new Coordinate(0, 0)));
    }

    [Fact]
    public void TakeTurn_WithoutTarget_StillAsksCurrentStrategyAndReportsTheResultOnce()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(9, 9)), new FakeShotStrategy()).Started();

        var result = game.Controller.TakeTurn();

        Assert.Equal(ShotResult.Miss, result);
        Assert.Equal(1, game.Strategy1.SelectTargetCalls);
        Assert.Single(game.Strategy1.Received);
        Assert.Same(game.Player2, game.Controller.GetCurrentPlayer());
    }
}
