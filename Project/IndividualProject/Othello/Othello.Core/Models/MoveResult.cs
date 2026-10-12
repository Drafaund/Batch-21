namespace Othello.Core;

public sealed record MoveResult(MoveFailureReason Failure, IReadOnlyList<Move> Moves)
{
    public bool IsSuccess() => Failure == MoveFailureReason.None;
}
