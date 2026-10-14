namespace Othello.Core.Tests;

using System.Reflection;

public class GameManagerMoveTests
{
    [Fact]
    public void TryPlayMove_WhenPositionOutsideBoard_ReturnsOutOfBoundsAndLeavesStateUnchanged()
    {
        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"));
        game.Start();
        var originalTurn = game.CurrentTurn;
        var originalScore = game.GetScore();

        var result = game.TryPlayMove(new Position(-1, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveFailureReason.OutOfBounds, result.Failure);
        Assert.Empty(result.Moves);
        Assert.Equal(originalTurn, game.CurrentTurn);
        Assert.Equal(originalScore, game.GetScore());
    }

    [Fact]
    public void TryPlayMove_WhenCellOccupied_ReturnsCellOccupiedAndLeavesStateUnchanged()
    {
        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"));
        game.Start();
        var originalTurn = game.CurrentTurn;
        var originalScore = game.GetScore();

        var result = game.TryPlayMove(new Position(3, 3));

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveFailureReason.CellOccupied, result.Failure);
        Assert.Empty(result.Moves);
        Assert.Equal(originalTurn, game.CurrentTurn);
        Assert.Equal(originalScore, game.GetScore());
    }

    [Fact]
    public void TryPlayMove_WhenNoFlips_ReturnsNoPiecesFlippedAndLeavesStateUnchanged()
    {
        var board = BoardBuilder.Build(
            "........",
            "........",
            "........",
            "...WB...",
            "...BW...",
            "........",
            "........",
            "........");
        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"), board, PieceColor.Black);
        game.Start();
        var originalTurn = game.CurrentTurn;
        var originalScore = game.GetScore();

        var result = game.TryPlayMove(new Position(0, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveFailureReason.NoPiecesFlipped, result.Failure);
        Assert.Empty(result.Moves);
        Assert.Equal(originalTurn, game.CurrentTurn);
        Assert.Equal(originalScore, game.GetScore());
    }

    [Fact]
    public void TryPlayMove_WhenGameOver_ReturnsGameOver()
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

        var property = typeof(GameManager).GetProperty(nameof(GameManager.Result), BindingFlags.Instance | BindingFlags.Public)!;
        var setter = property.GetSetMethod(true) ?? throw new InvalidOperationException("Result setter not found.");
        setter.Invoke(game, new object?[] { GameResult.BlackWins });

        var startedField = typeof(GameManager).GetField("_isStarted", BindingFlags.Instance | BindingFlags.NonPublic)!;
        startedField.SetValue(game, true);

        var result = game.TryPlayMove(new Position(0, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveFailureReason.GameOver, result.Failure);
    }

    [Fact]
    public void TryPlayMove_WhenReentrantCall_ReturnsReentrantCall()
    {
        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"));
        game.Start();

        MoveResult callbackResult = new(MoveFailureReason.None, Array.Empty<Move>());
        game.MovePlayed += _ =>
        {
            callbackResult = game.TryPlayMove(new Position(2, 3));
        };

        var outerResult = game.TryPlayMove(new Position(2, 3));

        Assert.Equal(MoveFailureReason.None, outerResult.Failure);
        Assert.Equal(MoveFailureReason.ReentrantCall, callbackResult.Failure);
    }
}
