namespace Othello.Core;

public sealed class RandomAIPlayer : Player, IAutoPlayer
{
    private readonly Random _random;

    public RandomAIPlayer(PieceColor color, string name, Random? random = null)
        : base(color, name)
    {
        _random = random ?? new Random();
    }

    public Move ChooseMove(IReadOnlyList<Move> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one candidate is required.", nameof(candidates));
        }

        return candidates[_random.Next(candidates.Count)];
    }
}
