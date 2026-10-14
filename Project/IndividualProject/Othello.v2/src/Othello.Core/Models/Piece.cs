namespace Othello.Core;

public sealed class Piece
{
    internal Piece(PieceColor color)
    {
        Color = color;
    }

    public PieceColor Color { get; internal set; }
}
