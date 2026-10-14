namespace Othello.Core;

public sealed class Cell
{
    internal Cell(Position position)
    {
        Position = position;
    }

    public Position Position { get; }
    public Piece? Piece { get; internal set; }
}
