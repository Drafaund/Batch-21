using Othello.Core;
using Spectre.Console;

namespace Othello.ConsoleApp;

public static class Program
{
    private static Board? _board;

    public static void Main()
    {
        AnsiConsole.Write(new Panel("[bold green]Welcome to Othello[/]")
        {
            Border = BoxBorder.Rounded
        });
        AnsiConsole.WriteLine();

        var game = BuildGame();
        var presenter = new GameConsolePresenter(new ConsoleBoardRenderer(), _board!);
        presenter.Subscribe(game);
        try
        {
            var start = game.Start();
            if (!start.IsSuccess())
            {
                AnsiConsole.MarkupLine($"[red]Unable to start:[/] {start.Failure}");
            }

            while (!game.IsGameOver())
            {
                var input = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter a move (for example [yellow]C4[/]), or [yellow]q[/] to quit:")
                        .AllowEmpty());
                if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase)) break;
                try
                {
                    var result = game.TryPlayMove(ParsePosition(input ?? string.Empty));
                    if (!result.IsSuccess())
                    {
                        AnsiConsole.MarkupLine($"[red]Move rejected:[/] {result.Failure}");
                    }
                }
                catch (FormatException)
                {
                    AnsiConsole.MarkupLine(
                        "[red]Invalid position.[/] Use a column [yellow]A-H[/] and row [yellow]1-8[/].");
                }
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
        return AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("Select a game mode:")
                .AddChoices(1, 2, 3, 4)
                .UseConverter(mode => mode switch
                {
                    1 => "Human vs Greedy AI",
                    2 => "Human vs Random AI",
                    3 => "Human vs Human",
                    4 => "AI vs AI",
                    _ => "Unknown"
                }));
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
