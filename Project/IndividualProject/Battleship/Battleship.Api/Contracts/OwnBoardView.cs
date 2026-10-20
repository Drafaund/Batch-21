namespace Battleship.Api;

/// <summary>The player's own board, indexed <c>Cells[y][x]</c> (row, then column).</summary>
internal sealed record OwnBoardView(int Size, OwnCell[][] Cells);
