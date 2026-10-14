namespace Othello.Core.Tests;

using System.Reflection;

public class GameManagerTurnTests
{
    [Fact]
    public void GetValidMoves_BeforeStart_ReturnsEmpty()
    {
        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"));

        Assert.Empty(game.GetValidMoves());
    }

    [Fact]
    public void TryPlayMove_WhenCurrentPlayerIsSkipped_CurrentPlayerKeepsTurn()
    {
        var board = BoardBuilder.Build(
            ".BW.....",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........");

        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"), board, PieceColor.White);
        game.Start();

        var result = game.TryPlayMove(new Position(0, 0));

        Assert.True(result.IsSuccess);
        Assert.Equal(PieceColor.White, game.CurrentTurn);
    }

    [Fact]
    public void GetValidMoves_AfterGameOver_ReturnsEmpty()
    {
        var board = BoardBuilder.Build(
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB",
            "BBBBBBBB");

        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"), board, PieceColor.Black);

        var startedField = typeof(GameManager).GetField("_isStarted", BindingFlags.Instance | BindingFlags.NonPublic)!;
        startedField.SetValue(game, true);

        var resultProperty = typeof(GameManager).GetProperty(nameof(GameManager.Result), BindingFlags.Instance | BindingFlags.Public)!;
        var setter = resultProperty.GetSetMethod(true) ?? throw new InvalidOperationException("Result setter was not found.");
        setter.Invoke(game, new object?[] { GameResult.BlackWins });

        Assert.True(game.IsGameOver());
        Assert.Empty(game.GetValidMoves());
        Assert.Equal(GameResult.BlackWins, game.Result);
    }
}
