namespace Battleship.Core;

public interface IBoard
{
    int BoardSize { get; }
    ICell[,] Cells { get; set; }
}
