using Othello.Core;

namespace Othello.ConsoleApp;

public sealed class ConsoleBoardRenderer : IBoardRenderer
{
    public void Render(IBoardReader board)
    {
        Console.WriteLine("  A B C D E F G H");
        for (var row = 0; row < board.Size; row++)
        {
            Console.Write($"{row + 1} ");
            for (var col = 0; col < board.Size; col++)
            {
                var symbol = board.GetColor(new Position(row, col)) switch
                {
                    DiscColor.Black => 'X',
                    DiscColor.White => 'O',
                    _ => '.'
                };
                Console.Write($"{symbol} ");
            }
            Console.WriteLine();
        }
    }
}
