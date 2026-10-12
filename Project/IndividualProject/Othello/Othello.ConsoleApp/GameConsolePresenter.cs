using Othello.Core;
using Spectre.Console;

namespace Othello.ConsoleApp;

public sealed class GameConsolePresenter(IBoardRenderer renderer, IBoardReader board)
{
    private readonly IBoardRenderer _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    private readonly IBoardReader _board = board ?? throw new ArgumentNullException(nameof(board));

    public void Subscribe(IGameEvents events)
    {
        ArgumentNullException.ThrowIfNull(events);
        events.GameStarted += HandleGameStarted;
        events.MovePlayed += HandleMovePlayed;
        events.ScoreChanged += HandleScoreChanged;
        events.TurnChanged += HandleTurnChanged;
        events.TurnSkipped += HandleTurnSkipped;
        events.GameOver += HandleGameOver;
    }

    public void Unsubscribe(IGameEvents events)
    {
        ArgumentNullException.ThrowIfNull(events);
        events.GameStarted -= HandleGameStarted;
        events.MovePlayed -= HandleMovePlayed;
        events.ScoreChanged -= HandleScoreChanged;
        events.TurnChanged -= HandleTurnChanged;
        events.TurnSkipped -= HandleTurnSkipped;
        events.GameOver -= HandleGameOver;
    }

    private void HandleGameStarted() => _renderer.Render(_board);
    private void HandleMovePlayed(Move move)
    {
        var color = move.Color == DiscColor.Black ? "black on white" : "white on black";
        AnsiConsole.MarkupLine(
            $"[{color}]{move.Color}[/] plays [yellow]{ToNotation(move.Target)}[/], " +
            $"flipped [green]{move.Flipped.Count}[/]");
        _renderer.Render(_board);
    }
    private void HandleScoreChanged(Score score)
    {
        AnsiConsole.MarkupLine(
            $"Score: [white on black] Black {score.Black} [/] - " +
            $"[black on white] White {score.White} [/]");
    }
    private void HandleTurnChanged(DiscColor color)
    {
        var style = color == DiscColor.Black ? "white on black" : "black on white";
        AnsiConsole.MarkupLine($"[{style}] {color} turn [/]");
    }
    private void HandleTurnSkipped(DiscColor color) =>
        AnsiConsole.MarkupLine($"[yellow]{color} has no valid move and is skipped.[/]");
    private void HandleGameOver(GameResult result)
    {
        AnsiConsole.Write(new Panel($"[bold yellow]Game over: {result}[/]")
        {
            Header = new PanelHeader("Othello"),
            Border = BoxBorder.Double
        });
        AnsiConsole.WriteLine();
    }
    private static string ToNotation(Position position) => $"{(char)('A' + position.Col)}{position.Row + 1}";
}
