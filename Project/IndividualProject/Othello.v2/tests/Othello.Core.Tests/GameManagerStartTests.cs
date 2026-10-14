namespace Othello.Core.Tests;

public class GameManagerStartTests
{
    [Fact]
    public void Start_WhenBoardNotStarted_SetsInitialPiecesAndReturnsNone()
    {
        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"));

        var result = game.Start();

        Assert.True(result.IsSuccess);
        Assert.Equal(MoveFailureReason.None, result.Failure);
        Assert.Empty(result.Moves);

        Assert.Equal(new PieceColor? (PieceColor.Black), game.CurrentTurn);
        Assert.True(game.Board.Cells[3 * game.Board.Size + 3].Piece is not null && game.Board.Cells[3 * game.Board.Size + 3].Piece!.Color == PieceColor.White);
        Assert.True(game.Board.Cells[4 * game.Board.Size + 4].Piece is not null && game.Board.Cells[4 * game.Board.Size + 4].Piece!.Color == PieceColor.White);
        Assert.True(game.Board.Cells[3 * game.Board.Size + 4].Piece is not null && game.Board.Cells[3 * game.Board.Size + 4].Piece!.Color == PieceColor.Black);
        Assert.True(game.Board.Cells[4 * game.Board.Size + 3].Piece is not null && game.Board.Cells[4 * game.Board.Size + 3].Piece!.Color == PieceColor.Black);
    }

    [Fact]
    public void Start_WhenAlreadyStarted_ReturnsAlreadyStarted()
    {
        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"));
        game.Start();

        var result = game.Start();

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveFailureReason.AlreadyStarted, result.Failure);
        Assert.Empty(result.Moves);
    }

    [Fact]
    public void TryPlayMove_BeforeStart_ReturnsNotStarted()
    {
        var game = new GameManager(new HumanPlayer(PieceColor.Black, "Black"), new HumanPlayer(PieceColor.White, "White"));

        var result = game.TryPlayMove(new Position(2, 3));

        Assert.False(result.IsSuccess);
        Assert.Equal(MoveFailureReason.NotStarted, result.Failure);
        Assert.Empty(result.Moves);
    }

    [Fact]
    public void GameManager_Constructor_WithWrongPlayerColors_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => new GameManager(new HumanPlayer(PieceColor.White, "White"), new HumanPlayer(PieceColor.White, "White")));
    }
}
