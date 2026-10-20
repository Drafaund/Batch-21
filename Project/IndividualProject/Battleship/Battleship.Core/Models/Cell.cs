namespace Battleship.Core;

public class Cell : ICell
{
    public Cell(Coordinate position)
    {
        Position = position;
        State = CellState.Empty;
    }

    public Coordinate Position { get; }
    public CellState State { get; set; }
    public IShip? OccupyingShip { get; set; }
    public bool IsOccupied => OccupyingShip != null;
}
