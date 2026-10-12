namespace Othello.Core;

public sealed class GreedyAIPlayer : Player, IAutoPlayer
{
    private readonly IMoveValidator _validator;
    public GreedyAIPlayer(DiscColor color, string name, IMoveValidator validator) : base(color, name) => _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    public Position ChooseMove(IBoardReader board, IReadOnlyList<Position> validMoves)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(validMoves);
        if (validMoves.Count == 0) throw new ArgumentException("At least one valid move is required.", nameof(validMoves));
        var best = validMoves[0];
        var bestCount = -1;
        foreach (var move in validMoves)
        {
            var count = _validator.GetFlips(board, move, Color).Count;
            if (count > bestCount) { best = move; bestCount = count; }
        }
        return best;
    }
}
