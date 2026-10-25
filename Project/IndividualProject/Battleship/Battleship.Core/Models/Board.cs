namespace Battleship.Core;

public class Board : IBoard
{
    public Board() //Otomatis berjlaan ketika objek dibuat
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

    // Hanya bisa di set diconstructor dan gak bisa diubah setelahnya
    public int BoardSize { get; } 

    public ICell[,] Cells { get; set; }
}
