using Othello.Core;
using Spectre.Console;

namespace Othello.ConsoleApp;

public sealed class ConsoleBoardRenderer : IBoardRenderer
{
    public void Render(IBoardReader board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var table = new Table
        {
            Border = TableBorder.Rounded,
            ShowRowSeparators = true
        };
        table.AddColumn(new TableColumn("[bold] [/]").Centered());

        for (var col = 0; col < board.Size; col++)
        {
            var label = ((char)('A' + col)).ToString();
            table.AddColumn(new TableColumn($"[bold]{label}[/]").Centered());
        }

        for (var row = 0; row < board.Size; row++)
        {
            var cells = new List<string> { $"[bold]{row + 1}[/]" };
            for (var col = 0; col < board.Size; col++)
            {
                var cell = board.GetColor(new Position(row, col)) switch
                {
                    DiscColor.Black => "[white on black] X [/]",
                    DiscColor.White => "[black on white] O [/]",
                    _ => "[grey].[/]"
                };
                cells.Add(cell);
            }
            table.AddRow(cells.ToArray());
        }

        AnsiConsole.Write(table);
    }
}
