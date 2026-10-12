namespace Othello.Core;

public interface IGameEndRule
{
    bool TryGetResult(IBoardReader board, out GameResult result);
}
