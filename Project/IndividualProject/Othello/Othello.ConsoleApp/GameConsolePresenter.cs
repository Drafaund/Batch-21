using Othello.Core;

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
        Console.WriteLine($"{move.Color} plays {ToNotation(move.Target)}, flipped {move.Flipped.Count}");
        _renderer.Render(_board);
    }
    private void HandleScoreChanged(Score score) => Console.WriteLine($"Score: Black {score.Black} - White {score.White}");
    private void HandleTurnChanged(DiscColor color) => Console.WriteLine($"{color} turn.");
    private void HandleTurnSkipped(DiscColor color) => Console.WriteLine($"{color} has no valid move and is skipped.");
    private void HandleGameOver(GameResult result) => Console.WriteLine($"Game over: {result}.");
    private static string ToNotation(Position position) => $"{(char)('A' + position.Col)}{position.Row + 1}";
}
