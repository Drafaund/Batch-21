namespace Othello.Core.Tests;

public sealed class ScriptedAutoPlayer : Player, IAutoPlayer
{
    private readonly Queue<Move> _moves;

    public ScriptedAutoPlayer(PieceColor color, string name, IEnumerable<Move>? moves = null, bool returnInvalidMove = false)
        : base(color, name)
    {
        _moves = new Queue<Move>(moves ?? Enumerable.Empty<Move>());
        ReturnInvalidMove = returnInvalidMove;
    }

    public bool ReturnInvalidMove { get; }

    public void Enqueue(Move move)
    {
        _moves.Enqueue(move);
    }

    public Move ChooseMove(IReadOnlyList<Move> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one candidate is required.", nameof(candidates));
        }

        if (_moves.Count > 0)
        {
            var scriptedMove = _moves.Dequeue();
            if (ReturnInvalidMove)
            {
                return new Move(new Position(-1, -1), Color, Array.Empty<Position>());
            }

            return scriptedMove;
        }

        if (ReturnInvalidMove)
        {
            return new Move(new Position(-1, -1), Color, Array.Empty<Position>());
        }

        return candidates[0];
    }
}
