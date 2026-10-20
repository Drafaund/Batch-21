namespace Battleship.Core;

/// <summary>A square board of cells; the standard board is 10 by 10.</summary>
public class Board : IBoard
{
    /// <summary>Creates a board of empty cells; <c>Cells[x, y]</c> is the cell at column x, row y.</summary>
    public Board()
    {
        BoardSize = 10;
        Cells = new ICell[BoardSize, BoardSize];
        for (var x = 0; x < BoardSize; x++)
        {
            for (var y = 0; y < BoardSize; y++)
            {
                Cells[x, y] = new Cell(new Coordinate(x, y));
            }
        }
    }

    /// <inheritdoc />
    public int BoardSize { get; }

    /// <summary>Indexed as <c>Cells[x, y]</c>: X is the column and Y is the row.</summary>
    public ICell[,] Cells { get; set; }
}
