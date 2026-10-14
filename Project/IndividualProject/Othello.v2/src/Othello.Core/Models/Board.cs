namespace Othello.Core;

public sealed class Board
{
    internal Board()
    {
        Size = 8;
        Cells = Enumerable
            .Range(0, Size * Size)
            .Select(index => new Cell(new Position(index / Size, index % Size)))
            .ToArray();
    }

    public int Size { get; }
    public IReadOnlyList<Cell> Cells { get; }
}
