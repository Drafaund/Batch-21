namespace Othello.Core;

public sealed class GreedyAIPlayer : Player, IAutoPlayer
{
    public GreedyAIPlayer(PieceColor color, string name)
        : base(color, name)
    {
    }

    public Move ChooseMove(IReadOnlyList<Move> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one candidate is required.", nameof(candidates));
        }

        var bestMove = candidates[0];
        for (var index = 1; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            if (candidate.Flipped.Count > bestMove.Flipped.Count)
            {
                bestMove = candidate;
            }
        }

        return bestMove;
    }
}
