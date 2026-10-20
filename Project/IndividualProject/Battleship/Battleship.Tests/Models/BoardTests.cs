namespace Battleship.Tests;

using Battleship.Core;

public class BoardTests
{
    [Fact]
    public void Constructor_NewBoard_HasBoardSizeTen()
    {
        var board = new Board();

        Assert.Equal(10, board.BoardSize);
    }

    [Fact]
    public void Constructor_NewBoard_HasTenByTenCells()
    {
        var board = new Board();

        Assert.Equal(2, board.Cells.Rank);
        Assert.Equal(10, board.Cells.GetLength(0));
        Assert.Equal(10, board.Cells.GetLength(1));
        Assert.Equal(100, board.Cells.Length);
    }

    [Fact]
    public void Constructor_NewBoard_EveryCellIsPresentWithMatchingPosition()
    {
        var board = new Board();

        for (var x = 0; x < board.BoardSize; x++)
        {
            for (var y = 0; y < board.BoardSize; y++)
            {
                Assert.NotNull(board.Cells[x, y]);
                Assert.Equal(new Coordinate(x, y), board.Cells[x, y].Position);
            }
        }
    }

    [Fact]
    public void Constructor_NewBoard_AllCellsAreEmptyAndUnoccupied()
    {
        var board = new Board();

        foreach (var cell in board.Cells)
        {
            Assert.Equal(CellState.Empty, cell.State);
            Assert.False(cell.IsOccupied);
        }
    }

    [Fact]
    public void Constructor_TwoBoards_DoNotShareCells()
    {
        var first = new Board();
        var second = new Board();

        first.Cells[0, 0].State = CellState.Miss;

        Assert.Equal(CellState.Empty, second.Cells[0, 0].State);
    }
}
