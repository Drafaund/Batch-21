namespace Othello.Core.Tests;

public class GameManagerEventTests
{
    [Fact]
    public void Start_WhenInvoked_RaisesExpectedEventsInOrder()
    {
        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"));
        var events = new List<string>();

        game.GameStarted += () => events.Add("GameStarted");
        game.ScoreChanged += _ => events.Add("ScoreChanged");
        game.TurnChanged += color => events.Add($"TurnChanged:{color}");

        game.Start();

        Assert.Equal(new[]
        {
            "GameStarted",
            "ScoreChanged",
            "TurnChanged:Black"
        }, events);
    }

    [Fact]
    public void TryPlayMove_WhenMoveSucceeds_RaisesMoveScoreAndTurnEventsInOrder()
    {
        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"));
        game.Start();

        var events = new List<string>();
        game.MovePlayed += _ => events.Add("MovePlayed");
        game.ScoreChanged += _ => events.Add("ScoreChanged");
        game.TurnChanged += color => events.Add($"TurnChanged:{color}");

        game.TryPlayMove(new Position(2, 3));

        Assert.Equal(new[]
        {
            "MovePlayed",
            "ScoreChanged",
            "TurnChanged:White"
        }, events);
    }

    [Fact]
    public void TryPlayMove_WhenCurrentPlayerPasses_RaisesTurnSkippedBeforeTurnChanged()
    {
        var board = BoardBuilder.Build(
            ".BWBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            ".BWBBBBB");

        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"), board, PieceColor.White);
        game.Start();

        var events = new List<string>();
        game.MovePlayed += _ => events.Add("MovePlayed");
        game.ScoreChanged += _ => events.Add("ScoreChanged");
        game.TurnSkipped += color => events.Add($"TurnSkipped:{color}");
        game.TurnChanged += color => events.Add($"TurnChanged:{color}");

        game.TryPlayMove(new Position(0, 0));

        Assert.Equal(new[]
        {
            "MovePlayed",
            "ScoreChanged",
            "TurnSkipped:Black",
            "TurnChanged:White"
        }, events);
    }

    [Fact]
    public void TryPlayMove_WhenGameEnds_RaisesGameOverAfterScoreChanged()
    {
        var board = BoardBuilder.Build(
            ".WBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB");

        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"), board, PieceColor.Black);
        game.Start();

        var events = new List<string>();
        game.MovePlayed += _ => events.Add("MovePlayed");
        game.ScoreChanged += _ => events.Add("ScoreChanged");
        game.GameOver += result => events.Add($"GameOver:{result}");

        game.TryPlayMove(new Position(0, 0));

        Assert.Equal(new[]
        {
            "MovePlayed",
            "ScoreChanged",
            "GameOver:BlackWins"
        }, events);
        Assert.Equal(new Score(64, 0), game.GetScore());
        Assert.Empty(game.GetValidMoves());
        Assert.True(game.Board.Cells.All(cell => cell.Piece is not null));
    }
}
