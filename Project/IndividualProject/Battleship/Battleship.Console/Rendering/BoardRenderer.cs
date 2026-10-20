using Battleship.Core;

namespace Battleship.ConsoleApp;

/// <summary>Draws boards as text. A board drawn without ships shows only hits and misses.</summary>
public class BoardRenderer
{
    private const string ColumnGap = "    ";
    private const string Legend = "Legend: ~ water   # ship   X hit   o miss";

    private readonly ConsoleScreen _screen;

    public BoardRenderer(ConsoleScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
    }

    /// <summary>The player's own fleet (with ships) next to the enemy board (hits and misses only).</summary>
    public void ShowPlayerView(IBoard ownBoard, IBoard enemyBoard)
    {
        ShowSideBySide("YOUR FLEET", Render(ownBoard, showShips: true), "TARGET BOARD", Render(enemyBoard, showShips: false));
    }

    /// <summary>Two fleets, both with ships. Only for bot games and the end of a game.</summary>
    public void ShowFleets(string leftTitle, IBoard left, string rightTitle, IBoard right)
    {
        ShowSideBySide(leftTitle, Render(left, showShips: true), rightTitle, Render(right, showShips: true));
    }

    public void ShowFleet(string title, IBoard board)
    {
        _screen.WriteLine(title);
        foreach (var line in Render(board, showShips: true))
        {
            _screen.WriteLine(line);
        }

        _screen.WriteLine(Legend);
    }

    private void ShowSideBySide(string leftTitle, IReadOnlyList<string> left, string rightTitle, IReadOnlyList<string> right)
    {
        var width = left.Max(line => line.Length);
        _screen.WriteLine(leftTitle.PadRight(width) + ColumnGap + rightTitle);
        for (var i = 0; i < left.Count; i++)
        {
            _screen.WriteLine(left[i].PadRight(width) + ColumnGap + right[i]);
        }

        _screen.WriteLine(Legend);
    }

    private static List<string> Render(IBoard board, bool showShips)
    {
        var size = board.BoardSize;
        var lines = new List<string>
        {
            "    " + string.Concat(Enumerable.Range(1, size).Select(column => column.ToString().PadRight(3))).TrimEnd(),
        };

        for (var y = 0; y < size; y++)
        {
            var cells = Enumerable.Range(0, size).Select(x => $"{Symbol(board.Cells[x, y], showShips)}  ");
            lines.Add($" {(char)('A' + y)}  " + string.Concat(cells).TrimEnd());
        }

        return lines;
    }

    private static char Symbol(ICell cell, bool showShips)
    {
        return cell.State switch
        {
            CellState.Hit => 'X',
            CellState.Miss => 'o',
            CellState.Ship when showShips => '#',
            _ => '~',
        };
    }
}
