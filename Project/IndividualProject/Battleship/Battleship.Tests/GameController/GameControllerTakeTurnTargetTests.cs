namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerTakeTurnTargetTests
{
    [Test]
    public void TakeTurnWithTarget_EmptyCell_ReturnsMissAndMarksCell()
    {
        var game = new TestGame().Started();

        var result = game.Controller.TakeTurn(new Coordinate(9, 9));

        Assert.That(result, Is.EqualTo(ShotResult.Miss));
        Assert.That(game.Player2.Board.Cells[9, 9].State, Is.EqualTo(CellState.Miss));
    }

    [Test]
    public void TakeTurnWithTarget_ShipCell_ReturnsHitAndCountsHit()
    {
        var game = new TestGame().Started();

        var result = game.Controller.TakeTurn(new Coordinate(0, 0));

        var carrier = game.Player2.Fleet.Ships.Single(s => s.Type == ShipType.Carrier);
        Assert.That(result, Is.EqualTo(ShotResult.Hit));
        Assert.That(carrier.HitCount, Is.EqualTo(1));
    }

    [Test]
    public void TakeTurnWithTarget_Fired_NeverAsksAStrategyForATarget()
    {
        var game = new TestGame().Started();

        game.Controller.TakeTurn(new Coordinate(9, 9));
        game.Controller.TakeTurn(new Coordinate(8, 8));

        Assert.That(game.Strategy1.SelectTargetCalls, Is.EqualTo(0));
        Assert.That(game.Strategy2.SelectTargetCalls, Is.EqualTo(0));
    }

    [Test]
    public void TakeTurnWithTarget_Fired_TellsOnlyTheCurrentPlayersStrategyTheResult()
    {
        var game = new TestGame().Started();

        game.Controller.TakeTurn(new Coordinate(0, 0));

        Assert.That(game.Strategy1.Received, Is.EqualTo(new[] { (new Coordinate(0, 0), ShotResult.Hit) }));
        Assert.That(game.Strategy2.Received, Is.Empty);
    }

    [Test]
    public void TakeTurnWithTarget_ValidShot_PassesTurnToOtherPlayer()
    {
        var game = new TestGame().Started();

        game.Controller.TakeTurn(new Coordinate(9, 9));

        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player2));
    }

    [Test]
    public void TakeTurnWithTarget_SecondPlayersTurn_HitsPlayer1BoardAndTellsStrategy2()
    {
        var game = new TestGame().Started();
        game.Controller.TakeTurn(new Coordinate(9, 9));

        var result = game.Controller.TakeTurn(new Coordinate(0, 0));

        Assert.That(result, Is.EqualTo(ShotResult.Hit));
        Assert.That(game.Player1.Board.Cells[0, 0].State, Is.EqualTo(CellState.Hit));
        Assert.That(game.Strategy2.Received, Is.EqualTo(new[] { (new Coordinate(0, 0), ShotResult.Hit) }));
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
    }

    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    [TestCase(10, 0)]
    [TestCase(0, 10)]
    public void TakeTurnWithTarget_OutsideBoard_ReturnsInvalidKeepsTurnAndTellsStrategy(int x, int y)
    {
        var game = new TestGame().Started();

        var result = game.Controller.TakeTurn(new Coordinate(x, y));

        Assert.That(result, Is.EqualTo(ShotResult.Invalid));
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
        Assert.That(game.Strategy1.Received, Is.EqualTo(new[] { (new Coordinate(x, y), ShotResult.Invalid) }));
    }

    [Test]
    public void TakeTurnWithTarget_CellAlreadyFiredAt_ReturnsInvalidAndKeepsTurn()
    {
        var game = new TestGame().Started();
        game.Controller.TakeTurn(new Coordinate(9, 9));
        game.Controller.TakeTurn(new Coordinate(9, 9));

        var result = game.Controller.TakeTurn(new Coordinate(9, 9));

        Assert.That(result, Is.EqualTo(ShotResult.Invalid));
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
    }

    [Test]
    public void TakeTurnWithTarget_Fired_RaisesOnShotResolvedWithTheCurrentPlayerAsShooter()
    {
        var game = new TestGame().Started();
        var calls = new List<(IPlayer Shooter, Coordinate Target, ShotResult Result)>();
        game.Controller.OnShotResolved += (shooter, target, result) => calls.Add((shooter, target, result));

        game.Controller.TakeTurn(new Coordinate(0, 0));
        game.Controller.TakeTurn(new Coordinate(9, 9));

        Assert.That(calls.Count, Is.EqualTo(2));
        Assert.That(calls[0].Shooter, Is.SameAs(game.Player1));
        Assert.That((calls[0].Target, calls[0].Result), Is.EqualTo((new Coordinate(0, 0), ShotResult.Hit)));
        Assert.That(calls[1].Shooter, Is.SameAs(game.Player2));
        Assert.That((calls[1].Target, calls[1].Result), Is.EqualTo((new Coordinate(9, 9), ShotResult.Miss)));
    }

    [Test]
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

        Assert.That(last, Is.EqualTo(ShotResult.Sunk));
        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.Finished));
        Assert.That(winner, Is.SameAs(game.Player1));
        Assert.That(endedCount, Is.EqualTo(1));
        Assert.That(game.Controller.GetRemainingShips(game.Player2), Is.EqualTo(0));
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
    }

    [Test]
    public void TakeTurnWithTarget_GameNotStarted_Throws()
    {
        var game = new TestGame();

        Assert.Throws<InvalidOperationException>(() => game.Controller.TakeTurn(new Coordinate(0, 0)));
    }

    [Test]
    public void TakeTurnWithTarget_AfterGameEnded_Throws()
    {
        var game = new TestGame().Started();
        game.Controller.EndGame(game.Player1);

        Assert.Throws<InvalidOperationException>(() => game.Controller.TakeTurn(new Coordinate(0, 0)));
    }

    [Test]
    public void TakeTurn_WithoutTarget_StillAsksCurrentStrategyAndReportsTheResultOnce()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(9, 9)), new FakeShotStrategy()).Started();

        var result = game.Controller.TakeTurn();

        Assert.That(result, Is.EqualTo(ShotResult.Miss));
        Assert.That(game.Strategy1.SelectTargetCalls, Is.EqualTo(1));
        Assert.That(game.Strategy1.Received, Has.Exactly(1).Items);
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player2));
    }
}
