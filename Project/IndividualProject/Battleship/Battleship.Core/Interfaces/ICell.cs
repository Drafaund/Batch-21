namespace Battleship.Core;

public interface ICell
{
    Coordinate Position { get; }
    CellState State { get; set; }
    IShip? OccupyingShip { get; set; }
    bool IsOccupied { get; }
}
