namespace Battleship.Core;

/// <summary>One cell of a board.</summary>
public interface ICell
{
    /// <summary>Where the cell is on the board.</summary>
    Coordinate Position { get; }
    /// <summary>What the cell currently shows.</summary>
    CellState State { get; set; }
    /// <summary>The ship on this cell, or null for water.</summary>
    IShip? OccupyingShip { get; set; }
    /// <summary>True when a ship is on this cell.</summary>
    bool IsOccupied { get; }
}
