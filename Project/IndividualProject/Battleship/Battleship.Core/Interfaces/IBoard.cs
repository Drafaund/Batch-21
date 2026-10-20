namespace Battleship.Core;

/// <summary>A square grid of cells owned by one player.</summary>
public interface IBoard
{
    /// <summary>The number of rows and of columns.</summary>
    int BoardSize { get; }
    /// <summary>The cells, indexed <c>Cells[x, y]</c>: X is the column and Y is the row.</summary>
    ICell[,] Cells { get; set; }
}
