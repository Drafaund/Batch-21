using Othello.Core;

namespace Othello.ConsoleApp;

public sealed class GameConsolePresenter
{
    private readonly Board _board;
    private readonly ConsoleBoardRenderer _renderer;

    public GameConsolePresenter(Board board, ConsoleBoardRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(renderer);

        _board = board;
        _renderer = renderer;
    }

    public void Subscribe(GameManager game)
    {
        ArgumentNullException.ThrowIfNull(game);

        game.GameStarted += HandleGameStarted;
        game.MovePlayed += HandleMovePlayed;
        game.ScoreChanged += HandleScoreChanged;
        game.TurnChanged += HandleTurnChanged;
        game.TurnSkipped += HandleTurnSkipped;
        game.GameOver += HandleGameOver;
    }

    public void Unsubscribe(GameManager game)
    {
        ArgumentNullException.ThrowIfNull(game);

        game.GameStarted -= HandleGameStarted;
        game.MovePlayed -= HandleMovePlayed;
        game.ScoreChanged -= HandleScoreChanged;
        game.TurnChanged -= HandleTurnChanged;
        game.TurnSkipped -= HandleTurnSkipped;
        game.GameOver -= HandleGameOver;
    }

    private void HandleGameStarted()
    {
        Console.WriteLine("Game started.");
        _renderer.Render(_board);
    }

    private void HandleMovePlayed(Move move)
    {
        Console.WriteLine($"{move.Color} played {FormatPosition(move.Target)} and flipped {move.Flipped.Count} piece(s).");
        _renderer.Render(_board);
    }

    private void HandleScoreChanged(Score score)
    {
        Console.WriteLine($"Score: Black {score.Black} - White {score.White}");
    }

    private void HandleTurnChanged(PieceColor color)
    {
        Console.WriteLine($"Turn: {color}");
    }

    private void HandleTurnSkipped(PieceColor color)
    {
        Console.WriteLine($"{color} has no valid move and is skipped.");
    }

    private void HandleGameOver(GameResult result)
    {
        Console.WriteLine($"Game over! Result: {result}");
    }

    private static string FormatPosition(Position position)
    {
        var column = (char)('a' + position.Col);
        return $"{column}{position.Row + 1}";
    }
}
