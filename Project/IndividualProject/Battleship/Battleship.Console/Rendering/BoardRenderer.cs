using Battleship.Core;
using Spectre.Console;

namespace Battleship.ConsoleApp;

public class BoardRenderer
{
    private const int SideBySideMinWidth = 100;
    private const string Legend = "[blue]~[/] water   [bold white]#[/] ship   [bold red]X[/] hit   [yellow]o[/] miss";

    private readonly ConsoleScreen _screen;

    public BoardRenderer(ConsoleScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
    }

    public void ShowPlayerView(IBoard ownBoard, IBoard enemyBoard)
    {
        ShowSideBySide(
            BuildTable(ownBoard, "Your fleet", showShips: true),
            BuildTable(enemyBoard, "Target board", showShips: false));
    }

    public void ShowFleets(string leftTitle, IBoard left, string rightTitle, IBoard right)
    {
        ShowSideBySide(
            BuildTable(left, Markup.Escape(leftTitle), showShips: true),
            BuildTable(right, Markup.Escape(rightTitle), showShips: true));
    }

    public void ShowFleet(string title, IBoard board)
    {
        _screen.Write(BuildTable(board, title, showShips: true));
        _screen.WriteLine(Legend);
    }

    private void ShowSideBySide(Table left, Table right)
    {
        if (_screen.Width >= SideBySideMinWidth)
        {
            var grid = new Grid().AddColumn().AddColumn();
            grid.AddRow(left, right);
            _screen.Write(grid);
        }
        else
        {
            _screen.Write(left);
            _screen.Write(right);
        }

        _screen.WriteLine(Legend);
    }

    private static Table BuildTable(IBoard board, string title, bool showShips)
    {
        var table = new Table().Border(TableBorder.Rounded).Title($"[bold]{title}[/]");
        table.AddColumn(new TableColumn(string.Empty));
        for (var column = 1; column <= board.BoardSize; column++)
        {
            table.AddColumn(new TableColumn($"[grey]{column}[/]").Centered());
        }

        for (var y = 0; y < board.BoardSize; y++)
        {
            var row = new List<string> { $"[grey]{(char)('A' + y)}[/]" };
            for (var x = 0; x < board.BoardSize; x++)
            {
                row.Add(Symbol(board.Cells[x, y], showShips));
            }

            table.AddRow(row.ToArray());
        }

        return table;
    }

    private static string Symbol(ICell cell, bool showShips)
    {
        return cell.State switch
        {
            CellState.Hit => "[bold red]X[/]",
            CellState.Miss => "[yellow]o[/]",
            CellState.Ship when showShips => "[bold white]#[/]",
            _ => "[blue]~[/]",
        };
    }
}
