using Othello.Core;

namespace Othello.ConsoleApp;

public static class Program
{
    private static Board? _board;

    public static void Main()
    {
        var game = BuildGame();
        var presenter = new GameConsolePresenter(new ConsoleBoardRenderer(), _board!);
        presenter.Subscribe(game);
        try
        {
            var start = game.Start();
            if (!start.IsSuccess()) Console.WriteLine($"Unable to start: {start.Failure}");
            while (!game.IsGameOver())
            {
                Console.Write("Enter a move (for example C4), or q to quit: ");
                var input = Console.ReadLine();
                if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase)) break;
                try
                {
                    var result = game.TryPlayMove(ParsePosition(input ?? string.Empty));
                    if (!result.IsSuccess()) Console.WriteLine($"Move rejected: {result.Failure}");
                }
                catch (FormatException) { Console.WriteLine("Invalid position. Use a column A-H and row 1-8."); }
            }
        }
        finally { presenter.Unsubscribe(game); }
    }

    private static GameManager BuildGame()
    {
        _board = new Board();
        var validator = new OthelloMoveValidator();
        var mode = ReadMode();
        IPlayer black = mode == 4
            ? new RandomAIPlayer(DiscColor.Black, "Black AI", new Random())
            : new HumanPlayer(DiscColor.Black, "Black");
        IPlayer white = mode switch
        {
            2 => new RandomAIPlayer(DiscColor.White, "White AI", new Random()),
            3 => new HumanPlayer(DiscColor.White, "White"),
            4 => new GreedyAIPlayer(DiscColor.White, "White AI", validator),
            _ => new GreedyAIPlayer(DiscColor.White, "White AI", validator)
        };
        return new GameManager(_board, validator, new StandardGameEndRule(validator), black, white);
    }

    private static int ReadMode()
    {
        Console.WriteLine("1 Human vs Greedy (default), 2 Human vs Random, 3 Human vs Human, 4 AI vs AI");
        Console.Write("Mode: ");
        return int.TryParse(Console.ReadLine(), out var mode) && mode is >= 1 and <= 4 ? mode : 1;
    }

    private static Position ParsePosition(string input)
    {
        if (input.Length != 2) throw new FormatException();
        var col = char.ToUpperInvariant(input[0]) - 'A';
        var row = input[1] - '1';
        if (col is < 0 or > 7 || row is < 0 or > 7) throw new FormatException();
        return new Position(row, col);
    }
}
