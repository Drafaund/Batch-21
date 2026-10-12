namespace Othello.Core;

public sealed class OthelloMoveValidator : IMoveValidator
{
    private readonly IReadOnlyList<Direction> _directions =
    [
        new(-1, -1), new(-1, 0), new(-1, 1), new(0, -1),
        new(0, 1), new(1, -1), new(1, 0), new(1, 1)
    ];

    public List<Position> GetFlips(IBoardReader board, Position position, DiscColor color)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (color == DiscColor.Empty || !board.IsInside(position) || board.GetColor(position) != DiscColor.Empty) return [];
        var opponent = color == DiscColor.Black ? DiscColor.White : DiscColor.Black;
        var result = new List<Position>();
        foreach (var direction in _directions)
        {
            var line = new List<Position>();
            var current = new Position(position.Row + direction.DRow, position.Col + direction.DCol);
            while (board.IsInside(current) && board.GetColor(current) == opponent)
            {
                line.Add(current);
                current = new Position(current.Row + direction.DRow, current.Col + direction.DCol);
            }
            if (line.Count > 0 && board.IsInside(current) && board.GetColor(current) == color) result.AddRange(line);
        }
        return result;
    }

    public List<Position> GetValidMoves(IBoardReader board, DiscColor color)
    {
        ArgumentNullException.ThrowIfNull(board);
        var result = new List<Position>();
        for (var row = 0; row < board.Size; row++)
            for (var col = 0; col < board.Size; col++)
            {
                var position = new Position(row, col);
                if (GetFlips(board, position, color).Count > 0) result.Add(position);
            }
        return result;
    }

    public bool HasValidMove(IBoardReader board, DiscColor color) => GetValidMoves(board, color).Count > 0;
}
