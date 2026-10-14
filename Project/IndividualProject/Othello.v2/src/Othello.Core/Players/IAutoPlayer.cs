namespace Othello.Core;

public interface IAutoPlayer
{
    Move ChooseMove(IReadOnlyList<Move> candidates);
}
