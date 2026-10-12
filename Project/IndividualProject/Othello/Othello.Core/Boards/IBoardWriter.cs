namespace Othello.Core;

public interface IBoardWriter
{
    void SetColor(Position position, DiscColor color);
    void Flip(IEnumerable<Position> positions, DiscColor color);
    void Reset();
}
