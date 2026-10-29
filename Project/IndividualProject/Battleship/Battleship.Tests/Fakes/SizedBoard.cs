namespace Battleship.Tests;

using Battleship.Core;

public class SizedBoard : IBoard
{
    public SizedBoard(int size)
    {
        BoardSize = size;
        Cells = new ICell[size, size];
        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                Cells[x, y] = new Cell(new Coordinate(x, y));
            }
        }
    }

    public int BoardSize { get; }

    public ICell[,] Cells { get; set; }
}
