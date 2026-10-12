namespace Othello.Core;

public sealed class Board : IBoard
{
    private readonly DiscColor[,] _grid = new DiscColor[8, 8];
    public int Size => 8;

    public Board() => Reset();

    public DiscColor GetColor(Position position)
    {
        Validate(position);
        return _grid[position.Row, position.Col];
    }

    public bool IsInside(Position position) => position.Row >= 0 && position.Row < Size && position.Col >= 0 && position.Col < Size;

    public int Count(DiscColor color)
    {
        var count = 0;
        for (var row = 0; row < Size; row++)
            for (var col = 0; col < Size; col++)
                if (_grid[row, col] == color) count++;
        return count;
    }

    public void SetColor(Position position, DiscColor color)
    {
        Validate(position);
        _grid[position.Row, position.Col] = color;
    }

    public void Flip(IEnumerable<Position> positions, DiscColor color)
    {
        ArgumentNullException.ThrowIfNull(positions);
        foreach (var position in positions) SetColor(position, color);
    }

    public void Reset()
    {
        Array.Clear(_grid);
        SetColor(new Position(3, 3), DiscColor.White);
        SetColor(new Position(4, 4), DiscColor.White);
        SetColor(new Position(4, 3), DiscColor.Black);
        SetColor(new Position(3, 4), DiscColor.Black);
    }

    private void Validate(Position position)
    {
        if (!IsInside(position)) throw new ArgumentOutOfRangeException(nameof(position));
    }
}
