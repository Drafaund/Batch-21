namespace Othello.Core;

public interface IAutoPlayer : IPlayer
{
    Position ChooseMove(IBoardReader board, IReadOnlyList<Position> validMoves);
}
