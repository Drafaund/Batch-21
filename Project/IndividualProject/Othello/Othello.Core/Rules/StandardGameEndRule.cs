namespace Othello.Core;

public sealed class StandardGameEndRule(IMoveValidator validator) : IGameEndRule
{
    private readonly IMoveValidator _validator = validator ?? throw new ArgumentNullException(nameof(validator));

    public bool TryGetResult(IBoardReader board, out GameResult result)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (_validator.HasValidMove(board, DiscColor.Black) || _validator.HasValidMove(board, DiscColor.White))
        {
            result = default;
            return false;
        }
        result = board.Count(DiscColor.Black) > board.Count(DiscColor.White)
            ? GameResult.BlackWins
            : board.Count(DiscColor.White) > board.Count(DiscColor.Black) ? GameResult.WhiteWins : GameResult.Draw;
        return true;
    }
}
