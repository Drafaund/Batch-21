namespace Battleship.Core;

/// <summary>One board cell: its position, its state, and the ship on it, if any.</summary>
public class Cell : ICell
{
    /// <summary>Creates an empty cell at <paramref name="position"/> that holds no ship.</summary>
    public Cell(Coordinate position)
    {
        Position = position;
        State = CellState.Empty;
    }

    /// <inheritdoc />
    public Coordinate Position { get; }
    /// <inheritdoc />
    public CellState State { get; set; }
    /// <inheritdoc />
    public IShip? OccupyingShip { get; set; }
    /// <inheritdoc />
    public bool IsOccupied => OccupyingShip != null;
}
