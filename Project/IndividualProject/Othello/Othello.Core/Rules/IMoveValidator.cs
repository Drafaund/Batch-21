namespace Othello.Core;

public interface IMoveValidator
{
    List<Position> GetFlips(IBoardReader board, Position position, DiscColor color);
    List<Position> GetValidMoves(IBoardReader board, DiscColor color);
    bool HasValidMove(IBoardReader board, DiscColor color);
}
