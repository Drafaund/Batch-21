namespace Battleship.Tests;

using Battleship.Core;

public class BoardTests
{
    [Test]
    public void Constructor_NewBoard_HasBoardSizeTen()
    {
        var board = new Board();

        Assert.That(board.BoardSize, Is.EqualTo(10));
    }

    [Test]
    public void Constructor_NewBoard_HasTenByTenCells()
    {
        var board = new Board();

        Assert.That(board.Cells.Rank, Is.EqualTo(2));
        Assert.That(board.Cells.GetLength(0), Is.EqualTo(10));
        Assert.That(board.Cells.GetLength(1), Is.EqualTo(10));
        Assert.That(board.Cells.Length, Is.EqualTo(100));
    }

    [Test]
    public void Constructor_NewBoard_EveryCellIsPresentWithMatchingPosition()
    {
        var board = new Board();

        for (var x = 0; x < board.BoardSize; x++)
        {
            for (var y = 0; y < board.BoardSize; y++)
            {
                Assert.That(board.Cells[x, y], Is.Not.Null);
                Assert.That(board.Cells[x, y].Position, Is.EqualTo(new Coordinate(x, y)));
            }
        }
    }

    [Test]
    public void Constructor_NewBoard_AllCellsAreEmptyAndUnoccupied()
    {
        var board = new Board();

        foreach (var cell in board.Cells)
        {
            Assert.That(cell.State, Is.EqualTo(CellState.Empty));
            Assert.That(cell.IsOccupied, Is.False);
        }
    }

    [Test]
    public void Constructor_TwoBoards_DoNotShareCells()
    {
        var first = new Board();
        var second = new Board();

        first.Cells[0, 0].State = CellState.Miss;

        Assert.That(second.Cells[0, 0].State, Is.EqualTo(CellState.Empty));
    }
}
