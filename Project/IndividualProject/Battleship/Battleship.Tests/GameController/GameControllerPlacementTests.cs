namespace Battleship.Tests;

using Battleship.Core;

public class GameControllerPlacementTests
{
    [Fact]
    public void ValidatePlaceShip_InsideBoardHorizontal_ReturnsTrue()
    {
        var game = new TestGame();

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(2, 3), Orientation.Horizontal);

        Assert.True(valid);
    }

    [Fact]
    public void ValidatePlaceShip_InsideBoardVertical_ReturnsTrue()
    {
        var game = new TestGame();

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(2, 3), Orientation.Vertical);

        Assert.True(valid);
    }

    [Theory]
    [InlineData(5, 0, Orientation.Horizontal)]
    [InlineData(0, 5, Orientation.Vertical)]
    public void ValidatePlaceShip_CarrierEndingOnLastCell_ReturnsTrue(int x, int y, Orientation orientation)
    {
        var game = new TestGame();

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(x, y), orientation);

        Assert.True(valid);
    }

    [Theory]
    [InlineData(6, 0, Orientation.Horizontal)]
    [InlineData(0, 6, Orientation.Vertical)]
    [InlineData(-1, 0, Orientation.Horizontal)]
    [InlineData(0, -1, Orientation.Vertical)]
    [InlineData(10, 0, Orientation.Vertical)]
    [InlineData(0, 10, Orientation.Horizontal)]
    public void ValidatePlaceShip_PartlyOutsideBoard_ReturnsFalse(int x, int y, Orientation orientation)
    {
        var game = new TestGame();

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(x, y), orientation);

        Assert.False(valid);
    }

    [Fact]
    public void ValidatePlaceShip_OverlapsPlacedShip_ReturnsFalse()
    {
        var game = new TestGame();
        game.Controller.PlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(0, 0), Orientation.Horizontal);

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Destroyer), new Coordinate(4, 0), Orientation.Vertical);

        Assert.False(valid);
    }

    [Fact]
    public void ValidatePlaceShip_TouchesButDoesNotOverlap_ReturnsTrue()
    {
        var game = new TestGame();
        game.Controller.PlaceShip(game.Player1, new Ship(ShipType.Carrier), new Coordinate(0, 0), Orientation.Horizontal);

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Destroyer), new Coordinate(5, 0), Orientation.Vertical);

        Assert.True(valid);
    }

    [Fact]
    public void ValidatePlaceShip_ShipTypeAlreadyInFleet_ReturnsFalse()
    {
        var game = new TestGame();
        game.Controller.PlaceShip(game.Player1, new Ship(ShipType.Cruiser), new Coordinate(0, 0), Orientation.Horizontal);

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Cruiser), new Coordinate(0, 5), Orientation.Horizontal);

        Assert.False(valid);
    }

    [Fact]
    public void ValidatePlaceShip_FleetAlreadyHasFiveShips_ReturnsFalse()
    {
        var game = new TestGame();
        game.PlaceFleet(game.Player1);

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Destroyer), new Coordinate(0, 9), Orientation.Horizontal);

        Assert.False(valid);
    }

    [Fact]
    public void ValidatePlaceShip_GameAlreadyStarted_ReturnsFalse()
    {
        var game = new TestGame().Started();

        var valid = game.Controller.ValidatePlaceShip(game.Player1, new Ship(ShipType.Destroyer), new Coordinate(0, 9), Orientation.Horizontal);

        Assert.False(valid);
    }

    [Fact]
    public void ValidatePlaceShip_ValidPlacement_ChangesNothing()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Carrier);

        game.Controller.ValidatePlaceShip(game.Player1, ship, new Coordinate(0, 0), Orientation.Horizontal);

        Assert.Empty(game.Player1.Fleet.Ships);
        Assert.Empty(ship.Positions);
        Assert.False(game.Player1.Board.Cells[0, 0].IsOccupied);
        Assert.Equal(CellState.Empty, game.Player1.Board.Cells[0, 0].State);
    }

    [Fact]
    public void ValidatePlaceShip_PlayerNotInGame_Throws()
    {
        var game = new TestGame();

        Assert.Throws<ArgumentException>(() =>
            game.Controller.ValidatePlaceShip(new FakePlayer(3, "Stranger"), new Ship(ShipType.Carrier), new Coordinate(0, 0), Orientation.Horizontal));
    }

    [Fact]
    public void PlaceShip_ValidHorizontal_FillsPositionsAndOrientation()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Cruiser);

        var placed = game.Controller.PlaceShip(game.Player1, ship, new Coordinate(2, 4), Orientation.Horizontal);

        Assert.True(placed);
        Assert.Equal(Orientation.Horizontal, ship.Orientation);
        Assert.Equal(new[] { new Coordinate(2, 4), new Coordinate(3, 4), new Coordinate(4, 4) }, ship.Positions);
    }

    [Fact]
    public void PlaceShip_ValidVertical_FillsPositionsAndOrientation()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Cruiser);

        var placed = game.Controller.PlaceShip(game.Player1, ship, new Coordinate(2, 4), Orientation.Vertical);

        Assert.True(placed);
        Assert.Equal(Orientation.Vertical, ship.Orientation);
        Assert.Equal(new[] { new Coordinate(2, 4), new Coordinate(2, 5), new Coordinate(2, 6) }, ship.Positions);
    }

    [Fact]
    public void PlaceShip_Valid_MarksCellsOnOwnBoardOnly()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Destroyer);

        game.Controller.PlaceShip(game.Player1, ship, new Coordinate(1, 1), Orientation.Horizontal);

        foreach (var position in ship.Positions)
        {
            var cell = game.Player1.Board.Cells[position.X, position.Y];
            Assert.Equal(CellState.Ship, cell.State);
            Assert.Same(ship, cell.OccupyingShip);
            Assert.True(cell.IsOccupied);
            Assert.False(game.Player2.Board.Cells[position.X, position.Y].IsOccupied);
        }

        Assert.Equal(2, game.Player1.Board.Cells.Cast<ICell>().Count(c => c.IsOccupied));
    }

    [Fact]
    public void PlaceShip_Valid_AddsShipToFleet()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Destroyer);

        game.Controller.PlaceShip(game.Player1, ship, new Coordinate(0, 0), Orientation.Horizontal);

        Assert.Same(ship, Assert.Single(game.Player1.Fleet.Ships));
        Assert.Empty(game.Player2.Fleet.Ships);
    }

    [Fact]
    public void PlaceShip_Invalid_ReturnsFalseAndChangesNothing()
    {
        var game = new TestGame();
        var ship = new Ship(ShipType.Carrier);

        var placed = game.Controller.PlaceShip(game.Player1, ship, new Coordinate(8, 0), Orientation.Horizontal);

        Assert.False(placed);
        Assert.Empty(ship.Positions);
        Assert.Empty(game.Player1.Fleet.Ships);
        Assert.DoesNotContain(game.Player1.Board.Cells.Cast<ICell>(), c => c.IsOccupied);
    }

    [Fact]
    public void StartGame_BothFleetsComplete_StateBecomesInProgress()
    {
        var game = new TestGame();
        game.PlaceFleet(game.Player1);
        game.PlaceFleet(game.Player2);

        game.Controller.StartGame();

        Assert.Equal(GameState.InProgress, game.Controller.GetState());
    }

    [Fact]
    public void StartGame_NoShipsPlaced_ThrowsAndStaysInSetup()
    {
        var game = new TestGame();

        Assert.Throws<InvalidOperationException>(() => game.Controller.StartGame());
        Assert.Equal(GameState.Setup, game.Controller.GetState());
    }

    [Fact]
    public void StartGame_OnlyOneFleetComplete_Throws()
    {
        var game = new TestGame();
        game.PlaceFleet(game.Player1);

        Assert.Throws<InvalidOperationException>(() => game.Controller.StartGame());
    }

    [Fact]
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

    [Fact]
    public void StartGame_CalledTwice_Throws()
    {
        var game = new TestGame().Started();

        Assert.Throws<InvalidOperationException>(() => game.Controller.StartGame());
    }
}
