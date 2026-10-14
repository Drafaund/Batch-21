using Othello.Core;

namespace Othello.ConsoleApp;

public sealed class ConsoleBoardRenderer
{
    public void Render(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);

        Console.WriteLine("  a b c d e f g h");
        for (var row = 0; row < board.Size; row++)
        {
            var line = $"{row + 1} ";
            for (var col = 0; col < board.Size; col++)
            {
                var cell = board.Cells[row * board.Size + col];
                var symbol = cell.Piece switch
                {
                    null => ".",
                    _ when cell.Piece.Color == PieceColor.Black => "B",
                    _ => "W"
                };

                line += $"{symbol} ";
            }

            Console.WriteLine(line.TrimEnd());
        }
    }
}
