namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerEdgeCaseTests
{
    private sealed class SubscriberException : Exception
    {
    }

    [Test]
    public void TakeTurn_OnShotResolvedThrows_ExceptionPropagatesAfterTheShotLanded()
    {
        var game = new TestGame(new FakeShotStrategy(new Coordinate(0, 0)), new FakeShotStrategy()).Started();
        game.Controller.OnShotResolved += (_, _, _) => throw new SubscriberException();

        Assert.Throws<SubscriberException>(() => game.Controller.TakeTurn());

        Assert.That(game.Player2.Board.Cells[0, 0].State, Is.EqualTo(CellState.Hit));
        Assert.That(game.Strategy1.Received, Is.Empty);
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.InProgress));
    }

    [Test]
    public void TakeTurn_OnShotResolvedThrew_RetryingTheSameTargetIsInvalid()
    {
        var game = new TestGame().Started();
        Action<IPlayer, Coordinate, ShotResult> failing = (_, _, _) => throw new SubscriberException();
        game.Controller.OnShotResolved += failing;
        Assert.Throws<SubscriberException>(() => game.Controller.TakeTurn(new Coordinate(0, 0)));
        game.Controller.OnShotResolved -= failing;

        var retry = game.Controller.TakeTurn(new Coordinate(0, 0));

        Assert.That(retry, Is.EqualTo(ShotResult.Invalid));
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
    }

    [Test]
    public void EndGame_OnGameEndedThrows_ExceptionPropagatesAndStateIsAlreadyFinished()
    {
        var game = new TestGame().Started();
        game.Controller.OnGameEnded += _ => throw new SubscriberException();

        Assert.Throws<SubscriberException>(() => game.Controller.EndGame(game.Player1));

        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.Finished));
        Assert.Throws<InvalidOperationException>(() => game.Controller.EndGame(game.Player1));
    }

    [Test]
    public void SwitchTurn_AfterGameEnded_StillAlternatesCurrentPlayer()
    {
        var game = new TestGame().Started();
        game.Controller.EndGame(game.Player1);

        game.Controller.SwitchTurn();

        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player2));
        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.Finished));
    }

    [Test]
    public void SwitchTurn_DuringGame_ChangesOnlyTheCurrentPlayer()
    {
        var game = new TestGame().Started();
        var events = 0;
        game.Controller.OnShotResolved += (_, _, _) => events++;
        game.Controller.OnGameEnded += _ => events++;

        game.Controller.SwitchTurn();

        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player2));
        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.InProgress));
        Assert.That(events, Is.EqualTo(0));
        Assert.That(game.Strategy1.SelectTargetCalls + game.Strategy2.SelectTargetCalls, Is.EqualTo(0));
    }

    [Test]
    public void ProcessShot_ShooterIsNotCurrentPlayer_StillResolvesAndKeepsTurn()
    {
        var game = new TestGame().Started();

        var result = game.Controller.ProcessShot(game.Player2, new Coordinate(0, 0));

        Assert.That(result, Is.EqualTo(ShotResult.Hit));
        Assert.That(game.Controller.GetCurrentPlayer(), Is.SameAs(game.Player1));
        Assert.That(game.Strategy2.Received, Is.Empty);
    }

    [TestCase(0, 0, Orientation.Horizontal, true)]
    [TestCase(0, 4, Orientation.Horizontal, true)]
    [TestCase(1, 0, Orientation.Horizontal, false)]
    [TestCase(0, 1, Orientation.Vertical, false)]
    [TestCase(5, 0, Orientation.Vertical, false)]
    public void ValidatePlaceShip_BoardSmallerThanTen_UsesTheBoardsOwnSize(int x, int y, Orientation orientation, bool expected)
    {
        var game = new TestGame(boardSize: 5);

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(x, y), orientation);

        Assert.That(valid, Is.EqualTo(expected));
    }

    [TestCase(4, 0, ShotResult.Hit)]
    [TestCase(4, 3, ShotResult.Miss)]
    [TestCase(5, 0, ShotResult.Invalid)]
    [TestCase(0, 5, ShotResult.Invalid)]
    [TestCase(7, 7, ShotResult.Invalid)]
    public void ProcessShot_BoardSmallerThanTen_UsesTheBoardsOwnSize(int x, int y, ShotResult expected)
    {
        var game = new TestGame(boardSize: 5).Started();

        var result = game.Controller.ProcessShot(game.Player1, new Coordinate(x, y));

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void PlaceShipsRandomly_BoardSmallerThanTen_KeepsEveryShipInsideIt(int seed)
    {
        var game = new TestGame(boardSize: 8);
        var ships = TestGame.AllTypes.Select(type => (IShip)new Ship(type)).ToList();

        game.Controller.PlaceShipsRandomly(game.Player1, ships, new Random(seed));

        var positions = game.Player1.Fleet.Ships.SelectMany(ship => ship.Positions).ToList();
        Assert.That(positions, Has.Count.EqualTo(17));
        Assert.That(positions, Has.All.Matches<Coordinate>(p => p.X is >= 0 and < 8 && p.Y is >= 0 and < 8));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void PlaceShip_ShipWithoutLength_IsAcceptedAndOccupiesNoCell(int length)
    {
        var game = new TestGame();
        var ship = new FakeShip(ShipType.Destroyer, length);

        var placed = game.Controller.PlaceShip(game.Player1, ship, new Coordinate(0, 0), Orientation.Horizontal);

        Assert.That(placed, Is.True);
        Assert.That(game.Player1.Fleet.Ships, Is.EqualTo(new[] { ship }));
        Assert.That(ship.Positions, Is.Empty);
        Assert.That(game.Player1.Board.Cells.Cast<ICell>().Any(c => c.IsOccupied), Is.False);
    }

    [Test]
    public void PlaceShip_SameShipForBothPlayers_IsAcceptedAndSharedBetweenFleets()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Destroyer);

        var first = game.Controller.PlaceShip(game.Player1, ship, new Coordinate(0, 0), Orientation.Horizontal);
        var second = game.Controller.PlaceShip(game.Player2, ship, new Coordinate(0, 5), Orientation.Horizontal);

        Assert.That(first, Is.True);
        Assert.That(second, Is.True);
        Assert.That(game.Player1.Fleet.Ships.Single(), Is.SameAs(ship));
        Assert.That(game.Player2.Fleet.Ships.Single(), Is.SameAs(ship));
        Assert.That(ship.Positions, Is.EqualTo(new[] { new Coordinate(0, 5), new Coordinate(1, 5) }));
    }
}
