namespace Othello.Core;

public sealed class RandomAIPlayer : Player, IAutoPlayer
{
    private readonly Random _random;
    public RandomAIPlayer(DiscColor color, string name, Random random) : base(color, name) => _random = random ?? throw new ArgumentNullException(nameof(random));
    public Position ChooseMove(IBoardReader board, IReadOnlyList<Position> validMoves)
    {
        ArgumentNullException.ThrowIfNull(validMoves);
        if (validMoves.Count == 0) throw new ArgumentException("At least one valid move is required.", nameof(validMoves));
        return validMoves[_random.Next(validMoves.Count)];
    }
}
