namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerShotTests
{
    [Test]
    public void ProcessShot_EmptyCell_ReturnsMissAndMarksCell()
    {
        var game = new TestGame().Started();

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(9, 9));

        Assert.That(result, Is.EqualTo(ShotResult.Miss));
        Assert.That(game.Player2.Board.Cells[9, 9].State, Is.EqualTo(CellState.Miss));
    }

    [Test]
    public void ProcessShot_ShipCell_ReturnsHitMarksCellAndCountsHit()
    {
        var game = new TestGame().Started();

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(0, 0));

        var carrier = game.Player2.Fleet.Ships.Single(s => s.Type == ShipType.Carrier);
        Assert.That(result, Is.EqualTo(ShotResult.Hit));
        Assert.That(game.Player2.Board.Cells[0, 0].State, Is.EqualTo(CellState.Hit));
        Assert.That(carrier.HitCount, Is.EqualTo(1));
        Assert.That(carrier.IsSunk, Is.False);
    }

    [Test]
    public void ProcessShot_LastCellOfShip_ReturnsSunk()
    {
        var game = new TestGame().Started();
        var results = TestGame.CellsOf(ShipType.Destroyer)
            .Select(c => game.Controller.ProcessShot(game.Player1, c))
            .ToList();

        Assert.That(results, Is.EqualTo(new[] { ShotResult.Hit, ShotResult.Sunk }));
        var destroyer = game.Player2.Fleet.Ships.Single(s => s.Type == ShipType.Destroyer);
        Assert.That(destroyer.IsSunk, Is.True);
        Assert.That(game.Player2.Board.Cells[1, 4].State, Is.EqualTo(CellState.Hit));
    }

    [Test]
    public void ProcessShot_SinkingOneShip_OtherShipsStayAfloat()
    {
        var game = new TestGame().Started();

        foreach (var cell in TestGame.CellsOf(ShipType.Destroyer))
        {
            game.Controller.ProcessShot(game.Player1, cell);
        }

        Assert.That(game.Controller.GetRemainingShips(game.Player2), Is.EqualTo(4));
        Assert.That(game.Controller.GetRemainingShips(game.Player1), Is.EqualTo(5));
    }

    [Test]
    public void ProcessShot_SameCellTwiceAfterMiss_ReturnsInvalid()
    {
        var game = new TestGame().Started();
        game.Controller.ProcessShot(game.Player1, new Coordinate(9, 9));

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(9, 9));

        Assert.That(result, Is.EqualTo(ShotResult.Invalid));
        Assert.That(game.Player2.Board.Cells[9, 9].State, Is.EqualTo(CellState.Miss));
    }

    [Test]
    public void ProcessShot_SameCellTwiceAfterHit_ReturnsInvalidAndDoesNotCountTwice()
    {
        var game = new TestGame().Started();
        game.Controller.ProcessShot(game.Player1, new Coordinate(0, 0));

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(0, 0));

        var carrier = game.Player2.Fleet.Ships.Single(s => s.Type == ShipType.Carrier);
        Assert.That(result, Is.EqualTo(ShotResult.Invalid));
        Assert.That(carrier.HitCount, Is.EqualTo(1));
    }

    [Test]
    public void ProcessShot_AlreadySunkCellShotAgain_ReturnsInvalid()
    {
        var game = new TestGame().Started();
        foreach (var cell in TestGame.CellsOf(ShipType.Destroyer))
        {
            game.Controller.ProcessShot(game.Player1, cell);
        }

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(1, 4));

        Assert.That(result, Is.EqualTo(ShotResult.Invalid));
    }

    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    [TestCase(10, 0)]
    [TestCase(0, 10)]
    [TestCase(99, 99)]
    public void ProcessShot_OutsideBoard_ReturnsInvalid(int x, int y)
    {
        var game = new TestGame().Started();

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(x, y));

        Assert.That(result, Is.EqualTo(ShotResult.Invalid));
    }

    [Test]
    public void ProcessShot_FromPlayer2_HitsPlayer1Board()
    {
        var game = new TestGame().Started();

        game.Controller.ProcessShot(game.Player2, new Coordinate(0, 0));

        Assert.That(game.Player1.Board.Cells[0, 0].State, Is.EqualTo(CellState.Hit));
        Assert.That(game.Player2.Board.Cells[0, 0].State, Is.EqualTo(CellState.Ship));
    }

    [Test]
    public void ProcessShot_Fired_RaisesOnShotResolvedWithShooterTargetAndResult()
    {
        var game = new TestGame().Started();
        var calls = new List<(IPlayer Shooter, Coordinate Target, ShotResult Result)>();
        game.Controller.OnShotResolved += (shooter, target, result) => calls.Add((shooter, target, result));

        game.Controller.ProcessShot(game.Player1, new Coordinate(0, 0));
        game.Controller.ProcessShot(game.Player2, new Coordinate(9, 9));

        Assert.That(calls.Count, Is.EqualTo(2));
        Assert.That(calls[0].Shooter, Is.SameAs(game.Player1));
        Assert.That(calls[0].Target, Is.EqualTo(new Coordinate(0, 0)));
        Assert.That(calls[0].Result, Is.EqualTo(ShotResult.Hit));
        Assert.That(calls[1].Shooter, Is.SameAs(game.Player2));
        Assert.That(calls[1].Target, Is.EqualTo(new Coordinate(9, 9)));
        Assert.That(calls[1].Result, Is.EqualTo(ShotResult.Miss));
    }

    [Test]
    public void ProcessShot_Invalid_StillRaisesOnShotResolved()
    {
        var game = new TestGame().Started();
        ShotResult? seen = null;
        game.Controller.OnShotResolved += (_, _, result) => seen = result;

        game.Controller.ProcessShot(game.Player1, new Coordinate(-1, -1));

        Assert.That(seen, Is.EqualTo(ShotResult.Invalid));
    }

    [Test]
    public void ProcessShot_SunkShip_RaisesOnShotResolvedWithSunk()
    {
        var game = new TestGame().Started();
        var results = new List<ShotResult>();
        game.Controller.OnShotResolved += (_, _, result) => results.Add(result);

        foreach (var cell in TestGame.CellsOf(ShipType.Destroyer))
        {
            game.Controller.ProcessShot(game.Player1, cell);
        }

        Assert.That(results, Is.EqualTo(new[] { ShotResult.Hit, ShotResult.Sunk }));
    }

    [Test]
    public void ProcessShot_GameNotStarted_Throws()
    {
        var game = new TestGame();

        Assert.Throws<InvalidOperationException>(() => game.Controller.ProcessShot(game.Player1, new Coordinate(0, 0)));
    }

    [Test]
    public void ProcessShot_ShooterNotInGame_Throws()
    {
        var game = new TestGame().Started();

        Assert.Throws<ArgumentException>(() => game.Controller.ProcessShot(new FakePlayer(3, "Stranger"), new Coordinate(0, 0)));
    }

    [Test]
    public void ProcessShot_NullShooter_Throws()
    {
        var game = new TestGame().Started();

        Assert.Throws<ArgumentNullException>(() => game.Controller.ProcessShot(null!, new Coordinate(0, 0)));
    }

    [Test]
    public void GetRemainingShips_BeforePlacement_IsZero()
    {
        var game = new TestGame();

        Assert.That(game.Controller.GetRemainingShips(game.Player1), Is.EqualTo(0));
    }

    [Test]
    public void GetRemainingShips_FullFleet_IsFive()
    {
        var game = new TestGame().Started();

        Assert.That(game.Controller.GetRemainingShips(game.Player1), Is.EqualTo(5));
        Assert.That(game.Controller.GetRemainingShips(game.Player2), Is.EqualTo(5));
    }

    [Test]
    public void GetRemainingShips_NullPlayer_Throws()
    {
        var game = new TestGame().Started();

        Assert.Throws<ArgumentNullException>(() => game.Controller.GetRemainingShips(null!));
    }
}
