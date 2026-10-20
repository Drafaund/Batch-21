namespace Battleship.Api;

/// <summary>The enemy board as the player may see it, indexed <c>Cells[y][x]</c>. Only shots are visible.</summary>
internal sealed record EnemyBoardView(int Size, EnemyCell[][] Cells);
