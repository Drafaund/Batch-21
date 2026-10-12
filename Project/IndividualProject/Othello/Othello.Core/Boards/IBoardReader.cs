namespace Othello.Core;

public interface IBoardReader
{
    int Size { get; }
    DiscColor GetColor(Position position);
    bool IsInside(Position position);
    int Count(DiscColor color);
}
