namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerShotTests
{
    [Fact]
    public void ProcessShot_EmptyCell_ReturnsMissAndMarksCell()
    {
        var game = new TestGame().Started();

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(9, 9));

        Assert.Equal(ShotResult.Miss, result);
        Assert.Equal(CellState.Miss, game.Player2.Board.Cells[9, 9].State);
    }

    [Fact]
    public void ProcessShot_ShipCell_ReturnsHitMarksCellAndCountsHit()
    {
        var game = new TestGame().Started();

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(0, 0));

        var carrier = game.Player2.Fleet.Ships.Single(s => s.Type == ShipType.Carrier);
        Assert.Equal(ShotResult.Hit, result);
        Assert.Equal(CellState.Hit, game.Player2.Board.Cells[0, 0].State);
        Assert.Equal(1, carrier.HitCount);
        Assert.False(carrier.IsSunk);
    }

    [Fact]
    public void ProcessShot_LastCellOfShip_ReturnsSunk()
    {
        var game = new TestGame().Started();
        var results = TestGame.CellsOf(ShipType.Destroyer)
            .Select(c => game.Controller.ProcessShot(game.Player1, c))
            .ToList();

        Assert.Equal(new[] { ShotResult.Hit, ShotResult.Sunk }, results);
        var destroyer = game.Player2.Fleet.Ships.Single(s => s.Type == ShipType.Destroyer);
        Assert.True(destroyer.IsSunk);
        Assert.Equal(CellState.Hit, game.Player2.Board.Cells[1, 4].State);
    }

    [Fact]
    public void ProcessShot_SinkingOneShip_OtherShipsStayAfloat()
    {
        var game = new TestGame().Started();

        foreach (var cell in TestGame.CellsOf(ShipType.Destroyer))
        {
            game.Controller.ProcessShot(game.Player1, cell);
        }

        Assert.Equal(4, game.Controller.GetRemainingShips(game.Player2));
        Assert.Equal(5, game.Controller.GetRemainingShips(game.Player1));
    }

    [Fact]
    public void ProcessShot_SameCellTwiceAfterMiss_ReturnsInvalid()
    {
        var game = new TestGame().Started();
        game.Controller.ProcessShot(game.Player1, new Coordinate(9, 9));

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(9, 9));

        Assert.Equal(ShotResult.Invalid, result);
        Assert.Equal(CellState.Miss, game.Player2.Board.Cells[9, 9].State);
    }

    [Fact]
    public void ProcessShot_SameCellTwiceAfterHit_ReturnsInvalidAndDoesNotCountTwice()
    {
        var game = new TestGame().Started();
        game.Controller.ProcessShot(game.Player1, new Coordinate(0, 0));

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(0, 0));

        var carrier = game.Player2.Fleet.Ships.Single(s => s.Type == ShipType.Carrier);
        Assert.Equal(ShotResult.Invalid, result);
        Assert.Equal(1, carrier.HitCount);
    }

    [Fact]
    public void ProcessShot_AlreadySunkCellShotAgain_ReturnsInvalid()
    {
        var game = new TestGame().Started();
        foreach (var cell in TestGame.CellsOf(ShipType.Destroyer))
        {
            game.Controller.ProcessShot(game.Player1, cell);
        }

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(1, 4));

        Assert.Equal(ShotResult.Invalid, result);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(10, 0)]
    [InlineData(0, 10)]
    [InlineData(99, 99)]
    public void ProcessShot_OutsideBoard_ReturnsInvalid(int x, int y)
    {
        var game = new TestGame().Started();

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(x, y));

        Assert.Equal(ShotResult.Invalid, result);
    }

    [Fact]
    public void ProcessShot_FromPlayer2_HitsPlayer1Board()
    {
        var game = new TestGame().Started();

        game.Controller.ProcessShot(game.Player2, new Coordinate(0, 0));

        Assert.Equal(CellState.Hit, game.Player1.Board.Cells[0, 0].State);
        Assert.Equal(CellState.Ship, game.Player2.Board.Cells[0, 0].State);
    }

    [Fact]
    public void ProcessShot_Fired_RaisesOnShotResolvedWithShooterTargetAndResult()
    {
        var game = new TestGame().Started();
        var calls = new List<(IPlayer Shooter, Coordinate Target, ShotResult Result)>();
        game.Controller.OnShotResolved += (shooter, target, result) => calls.Add((shooter, target, result));

        game.Controller.ProcessShot(game.Player1, new Coordinate(0, 0));
        game.Controller.ProcessShot(game.Player2, new Coordinate(9, 9));

        Assert.Equal(2, calls.Count);
        Assert.Same(game.Player1, calls[0].Shooter);
        Assert.Equal(new Coordinate(0, 0), calls[0].Target);
        Assert.Equal(ShotResult.Hit, calls[0].Result);
        Assert.Same(game.Player2, calls[1].Shooter);
        Assert.Equal(new Coordinate(9, 9), calls[1].Target);
        Assert.Equal(ShotResult.Miss, calls[1].Result);
    }

    [Fact]
    public void ProcessShot_Invalid_StillRaisesOnShotResolved()
    {
        var game = new TestGame().Started();
        ShotResult? seen = null;
        game.Controller.OnShotResolved += (_, _, result) => seen = result;

        game.Controller.ProcessShot(game.Player1, new Coordinate(-1, -1));

        Assert.Equal(ShotResult.Invalid, seen);
    }

    [Fact]
    public void ProcessShot_SunkShip_RaisesOnShotResolvedWithSunk()
    {
        var game = new TestGame().Started();
        var results = new List<ShotResult>();
        game.Controller.OnShotResolved += (_, _, result) => results.Add(result);

        foreach (var cell in TestGame.CellsOf(ShipType.Destroyer))
        {
            game.Controller.ProcessShot(game.Player1, cell);
        }

        Assert.Equal(new[] { ShotResult.Hit, ShotResult.Sunk }, results);
    }

    [Fact]
    public void ProcessShot_GameNotStarted_Throws()
    {
        var game = new TestGame();

        Assert.Throws<InvalidOperationException>(() => game.Controller.ProcessShot(game.Player1, new Coordinate(0, 0)));
    }

    [Fact]
    public void ProcessShot_ShooterNotInGame_Throws()
    {
        var game = new TestGame().Started();

        Assert.Throws<ArgumentException>(() => game.Controller.ProcessShot(new FakePlayer(3, "Stranger"), new Coordinate(0, 0)));
    }

    [Fact]
    public void GetRemainingShips_BeforePlacement_IsZero()
    {
        var game = new TestGame();

        Assert.Equal(0, game.Controller.GetRemainingShips(game.Player1));
    }

    [Fact]
    public void GetRemainingShips_FullFleet_IsFive()
    {
        var game = new TestGame().Started();

        Assert.Equal(5, game.Controller.GetRemainingShips(game.Player1));
        Assert.Equal(5, game.Controller.GetRemainingShips(game.Player2));
    }
}
