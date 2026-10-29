namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerPlacementTests
{
    [Test]
    public void ValidatePlaceShip_InsideBoardHorizontal_ReturnsTrue()
    {
        var game = new TestGame();

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(2, 3), Orientation.Horizontal);

        Assert.That(valid, Is.True);
    }

    [Test]
    public void ValidatePlaceShip_InsideBoardVertical_ReturnsTrue()
    {
        var game = new TestGame();

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(2, 3), Orientation.Vertical);

        Assert.That(valid, Is.True);
    }

    [TestCase(5, 0, Orientation.Horizontal)]
    [TestCase(0, 5, Orientation.Vertical)]
    public void ValidatePlaceShip_CarrierEndingOnLastCell_ReturnsTrue(int x, int y, Orientation orientation)
    {
        var game = new TestGame();

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(x, y), orientation);

        Assert.That(valid, Is.True);
    }

    [TestCase(6, 0, Orientation.Horizontal)]
    [TestCase(0, 6, Orientation.Vertical)]
    [TestCase(-1, 0, Orientation.Horizontal)]
    [TestCase(0, -1, Orientation.Vertical)]
    [TestCase(10, 0, Orientation.Vertical)]
    [TestCase(0, 10, Orientation.Horizontal)]
    public void ValidatePlaceShip_PartlyOutsideBoard_ReturnsFalse(int x, int y, Orientation orientation)
    {
        var game = new TestGame();

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(x, y), orientation);

        Assert.That(valid, Is.False);
    }

    [Test]
    public void ValidatePlaceShip_OverlapsPlacedShip_ReturnsFalse()
    {
        var game = new TestGame();
        game.Controller.PlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(0, 0), Orientation.Horizontal);

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Destroyer), new Coordinate(4, 0), Orientation.Vertical);

        Assert.That(valid, Is.False);
    }

    [Test]
    public void ValidatePlaceShip_TouchesButDoesNotOverlap_ReturnsTrue()
    {
        var game = new TestGame();
        game.Controller.PlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(0, 0), Orientation.Horizontal);

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Destroyer), new Coordinate(5, 0), Orientation.Vertical);

        Assert.That(valid, Is.True);
    }

    [Test]
    public void ValidatePlaceShip_ShipTypeAlreadyInFleet_ReturnsFalse()
    {
        var game = new TestGame();
        game.Controller.PlaceShip(game.Player1, new Ship(ShipType.Cruiser), new Coordinate(0, 0), Orientation.Horizontal);

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Cruiser), new Coordinate(0, 5), Orientation.Horizontal);

        Assert.That(valid, Is.False);
    }

    [Test]
    public void ValidatePlaceShip_FleetAlreadyHasFiveShips_ReturnsFalse()
    {
        var game = new TestGame();
        game.PlaceFleet(game.Player1);

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Destroyer), new Coordinate(0, 9), Orientation.Horizontal);

        Assert.That(valid, Is.False);
    }

    [Test]
    public void ValidatePlaceShip_GameAlreadyStarted_ReturnsFalse()
    {
        var game = new TestGame().Started();

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Destroyer), new Coordinate(0, 9), Orientation.Horizontal);

        Assert.That(valid, Is.False);
    }

    [Test]
    public void ValidatePlaceShip_ValidPlacement_ChangesNothing()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Carrier);

        game.Controller.ValidatePlaceShip(game.Player1, ship, new Coordinate(0, 0), Orientation.Horizontal);

        Assert.That(game.Player1.Fleet.Ships, Is.Empty);
        Assert.That(ship.Positions, Is.Empty);
        Assert.That(game.Player1.Board.Cells[0, 0].IsOccupied, Is.False);
        Assert.That(game.Player1.Board.Cells[0, 0].State, Is.EqualTo(CellState.Empty));
    }

    [Test]
    public void ValidatePlaceShip_PlayerNotInGame_Throws()
    {
        var game = new TestGame();

        Assert.Throws<ArgumentException>(() =>
            game.Controller.ValidatePlaceShip(new FakePlayer(3, "Stranger"), new Ship(ShipType.Carrier), new Coordinate(0, 0), Orientation.Horizontal));
    }

    [Test]
    public void PlaceShip_ValidHorizontal_FillsPositionsAndOrientation()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Cruiser);

        var placed = game.Controller.PlaceShip(game.Player1, ship, new Coordinate(2, 4), Orientation.Horizontal);

        Assert.That(placed, Is.True);
        Assert.That(ship.Orientation, Is.EqualTo(Orientation.Horizontal));
        Assert.That(ship.Positions, Is.EqualTo(new[] { new Coordinate(2, 4), new Coordinate(3, 4), new Coordinate(4, 4) }));
    }

    [Test]
    public void PlaceShip_ValidVertical_FillsPositionsAndOrientation()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Cruiser);

        var placed = game.Controller.PlaceShip(game.Player1, ship, new Coordinate(2, 4), Orientation.Vertical);

        Assert.That(placed, Is.True);
        Assert.That(ship.Orientation, Is.EqualTo(Orientation.Vertical));
        Assert.That(ship.Positions, Is.EqualTo(new[] { new Coordinate(2, 4), new Coordinate(2, 5), new Coordinate(2, 6) }));
    }

    [Test]
    public void PlaceShip_Valid_MarksCellsOnOwnBoardOnly()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Destroyer);

        game.Controller.PlaceShip(game.Player1, ship, new Coordinate(1, 1), Orientation.Horizontal);

        foreach (var position in ship.Positions)
        {
            var cell = game.Player1.Board.Cells[position.X, position.Y];
            Assert.That(cell.State, Is.EqualTo(CellState.Ship));
            Assert.That(cell.OccupyingShip, Is.SameAs(ship));
            Assert.That(cell.IsOccupied, Is.True);
            Assert.That(game.Player2.Board.Cells[position.X, position.Y].IsOccupied, Is.False);
        }

        Assert.That(game.Player1.Board.Cells.Cast<ICell>().Count(c => c.IsOccupied), Is.EqualTo(2));
    }

    [Test]
    public void PlaceShip_Valid_AddsShipToFleet()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Destroyer);

        game.Controller.PlaceShip(game.Player1, ship, new Coordinate(0, 0), Orientation.Horizontal);

        Assert.That(game.Player1.Fleet.Ships, Has.Exactly(1).Items);
        Assert.That(game.Player1.Fleet.Ships[0], Is.SameAs(ship));
        Assert.That(game.Player2.Fleet.Ships, Is.Empty);
    }

    [Test]
    public void PlaceShip_Invalid_ReturnsFalseAndChangesNothing()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Carrier);

        var placed = game.Controller.PlaceShip(game.Player1, ship, new Coordinate(8, 0), Orientation.Horizontal);

        Assert.That(placed, Is.False);
        Assert.That(ship.Positions, Is.Empty);
        Assert.That(game.Player1.Fleet.Ships, Is.Empty);
        Assert.That(game.Player1.Board.Cells.Cast<ICell>().Any(c => c.IsOccupied), Is.False);
    }

    [Test]
    public void StartGame_BothFleetsComplete_StateBecomesInProgress()
    {
        var game = new TestGame();
        game.PlaceFleet(game.Player1);
        game.PlaceFleet(game.Player2);

        game.Controller.StartGame();

        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.InProgress));
    }

    [Test]
    public void StartGame_NoShipsPlaced_ThrowsAndStaysInSetup()
    {
        var game = new TestGame();

        Assert.Throws<InvalidOperationException>(() => game.Controller.StartGame());
        Assert.That(game.Controller.GetState(), Is.EqualTo(GameState.Setup));
    }

    [Test]
    public void StartGame_OnlyOneFleetComplete_Throws()
    {
        var game = new TestGame();
        game.PlaceFleet(game.Player1);

        Assert.Throws<InvalidOperationException>(() => game.Controller.StartGame());
    }

    [Test]
    public void StartGame_FleetMissingOneShip_Throws()
    {
        var game = new TestGame();
        game.PlaceFleet(game.Player1);
        game.Controller.PlaceShip(game.Player2, new Ship(ShipType.Carrier), new Coordinate(0, 0), Orientation.Horizontal);
        game.Controller.PlaceShip(game.Player2, new Ship(ShipType.Battleship), new Coordinate(0, 1), Orientation.Horizontal);
        game.Controller.PlaceShip(game.Player2, new Ship(ShipType.Cruiser), new Coordinate(0, 2), Orientation.Horizontal);
        game.Controller.PlaceShip(game.Player2, new Ship(ShipType.Submarine), new Coordinate(0, 3), Orientation.Horizontal);

        Assert.Throws<InvalidOperationException>(() => game.Controller.StartGame());
    }

    [Test]
    public void StartGame_CalledTwice_Throws()
    {
        var game = new TestGame().Started();

        Assert.Throws<InvalidOperationException>(() => game.Controller.StartGame());
    }
}
