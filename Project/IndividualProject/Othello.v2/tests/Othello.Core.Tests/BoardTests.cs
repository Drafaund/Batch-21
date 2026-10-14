namespace Othello.Core.Tests;

public class BoardTests
{
    [Fact]
    public void Board_NewBoard_Has64CellsAndRowMajorPositionsAndEmptyCells()
    {
        var board = new Board();

        Assert.Equal(8, board.Size);
        Assert.Equal(64, board.Cells.Count);

        for (var row = 0; row < board.Size; row++)
        {
            for (var col = 0; col < board.Size; col++)
            {
                var index = row * board.Size + col;
                var cell = board.Cells[index];

                Assert.Equal(new Position(row, col), cell.Position);
                Assert.Null(cell.Piece);
            }
        }
    }
}
