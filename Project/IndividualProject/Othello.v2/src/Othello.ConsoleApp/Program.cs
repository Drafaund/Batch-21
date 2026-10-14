using Othello.Core;

namespace Othello.ConsoleApp;

public static class Program
{
    public static void Main()
    {
        var mode = ShowMenu();
        var game = BuildGame(mode);
        var presenter = new GameConsolePresenter(game.Board, new ConsoleBoardRenderer());
        presenter.Subscribe(game);

        try
        {
            var startResult = game.Start();
            if (!startResult.IsSuccess)
            {
                Console.WriteLine($"Unable to start game: {startResult.Failure}");
                return;
            }

            while (!game.IsGameOver())
            {
                var current = game.CurrentTurn;
                Console.Write($"{current} move (for example d3): ");
                var input = Console.ReadLine();
                if (input is null)
                {
                    Console.WriteLine("Input ended. Exiting game.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Please enter a move.");
                    continue;
                }

                try
                {
                    var position = ParsePosition(input);
                    var result = game.TryPlayMove(position);
                    if (!result.IsSuccess)
                    {
                        Console.WriteLine(GetFailureMessage(result.Failure));
                    }
                }
                catch (FormatException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine($"Final result: {game.Result}");
        }
        finally
        {
            presenter.Unsubscribe(game);
        }
    }

    private static int ShowMenu()
    {
        while (true)
        {
            Console.WriteLine("Choose game mode:");
            Console.WriteLine("1. Human vs Human");
            Console.WriteLine("2. Human vs Random AI");
            Console.WriteLine("3. Human vs Greedy AI");
            Console.WriteLine("4. AI vs AI");
            Console.Write("Your choice: ");

            var input = Console.ReadLine();
            if (int.TryParse(input, out var choice) && choice is >= 1 and <= 4)
            {
                return choice;
            }

            Console.WriteLine("Invalid selection. Please choose 1-4.");
        }
    }

    private static GameManager BuildGame(int mode)
    {
        return mode switch
        {
            1 => new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White")),
            2 => new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new RandomAIPlayer(PieceColor.White, "White")),
            3 => new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new GreedyAIPlayer(PieceColor.White, "White")),
            4 => new GameManager(new RandomAIPlayer(PieceColor.Black, "Black"), new RandomAIPlayer(PieceColor.White, "White")),
            _ => throw new InvalidOperationException($"Unsupported game mode: {mode}")
        };
    }

    private static Position ParsePosition(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.Length < 2)
        {
            throw new FormatException("Input must be a column letter followed by a row number, for example d3.");
        }

        var column = char.ToLowerInvariant(trimmed[0]);
        if (column < 'a' || column > 'h')
        {
            throw new FormatException("Column must be between a and h.");
        }

        if (!int.TryParse(trimmed.Substring(1), out var rowNumber) || rowNumber is < 1 or > 8)
        {
            throw new FormatException("Row must be between 1 and 8.");
        }

        return new Position(rowNumber - 1, column - 'a');
    }

    private static string GetFailureMessage(MoveFailureReason failure)
    {
        return failure switch
        {
            MoveFailureReason.NotStarted => "The game has not started yet.",
            MoveFailureReason.AlreadyStarted => "The game is already running.",
            MoveFailureReason.GameOver => "The game is over.",
            MoveFailureReason.OutOfBounds => "That move is outside the board.",
            MoveFailureReason.CellOccupied => "That cell is already occupied.",
            MoveFailureReason.NoPiecesFlipped => "That move does not flip any opponent pieces.",
            MoveFailureReason.ReentrantCall => "A move is already in progress.",
            _ => "The move could not be played."
        };
    }
}
