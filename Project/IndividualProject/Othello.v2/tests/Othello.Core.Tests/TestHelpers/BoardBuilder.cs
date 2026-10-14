namespace Othello.Core.Tests;

public static class BoardBuilder
{
    public static Board Build(params string[] rows)
    {
        if (rows.Length != 8)
        {
            throw new ArgumentException("Board must contain exactly 8 rows.", nameof(rows));
        }

        var board = new Board();

        for (var row = 0; row < rows.Length; row++)
        {
            var content = rows[row];
            if (content.Length != 8)
            {
                throw new ArgumentException($"Row {row} must contain exactly 8 characters.", nameof(rows));
            }

            for (var col = 0; col < content.Length; col++)
            {
                var cell = board.Cells[row * board.Size + col];
                var piece = ParsePiece(content[col]);
                cell.Piece = piece;
            }
        }

        return board;
    }

    private static Piece? ParsePiece(char symbol)
    {
        return symbol switch
        {
            '.' => null,
            'B' => new Piece(PieceColor.Black),
            'W' => new Piece(PieceColor.White),
            _ => throw new ArgumentException($"Unsupported board character '{symbol}'.", nameof(symbol))
        };
    }
}
