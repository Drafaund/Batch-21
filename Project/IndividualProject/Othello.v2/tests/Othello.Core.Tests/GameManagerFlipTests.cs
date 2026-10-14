namespace Othello.Core.Tests;

public class GameManagerFlipTests
{
    [Fact]
    public void GetValidMoves_StartingBoardForBlack_ReturnsExactlyTheFourInitialMoves()
    {
        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"));
        game.Start();

        var validMoves = game.GetValidMoves();

        Assert.Equal(new[]
        {
            new Position(2, 3),
            new Position(3, 2),
            new Position(4, 5),
            new Position(5, 4)
        }, validMoves);
    }

    [Fact]
    public void TryPlayMove_WhenMoveFlipsPieces_UpdatesBoardAndScore()
    {
        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"));
        game.Start();

        var result = game.TryPlayMove(new Position(2, 3));

        Assert.True(result.IsSuccess);
        Assert.Equal(MoveFailureReason.None, result.Failure);
        Assert.Single(result.Moves);
        Assert.Equal(new Position(2, 3), result.Moves[0].Target);
        Assert.Contains(new Position(3, 3), result.Moves[0].Flipped);
        Assert.Equal(PieceColor.Black, game.Board.Cells[3 * game.Board.Size + 3].Piece!.Color);
        Assert.Equal(new Score(4, 1), game.GetScore());
        Assert.Equal(PieceColor.White, game.CurrentTurn);
    }

    [Fact]
    public void TryPlayMove_WhenMoveFlipsInMultipleDirections_FlipsAllPiecesInThatMove()
    {
        var board = BoardBuilder.Build(
            "........",
            "...B....",
            "...W....",
            ".BW.....",
            "....W...",
            ".....B..",
            "........",
            "........");

        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"), board, PieceColor.Black);
        game.Start();

        var result = game.TryPlayMove(new Position(3, 3));

        Assert.True(result.IsSuccess);
        Assert.Equal(MoveFailureReason.None, result.Failure);
        Assert.Single(result.Moves);
        Assert.Equal(3, result.Moves[0].Flipped.Count);
        Assert.Contains(new Position(2, 3), result.Moves[0].Flipped);
        Assert.Contains(new Position(3, 2), result.Moves[0].Flipped);
        Assert.Contains(new Position(4, 4), result.Moves[0].Flipped);
        Assert.Equal(PieceColor.Black, game.Board.Cells[3 * game.Board.Size + 3].Piece!.Color);
        Assert.Equal(PieceColor.Black, game.Board.Cells[2 * game.Board.Size + 3].Piece!.Color);
        Assert.Equal(PieceColor.Black, game.Board.Cells[3 * game.Board.Size + 2].Piece!.Color);
        Assert.Equal(PieceColor.Black, game.Board.Cells[4 * game.Board.Size + 4].Piece!.Color);
    }
}
