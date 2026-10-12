using Othello.Core;

namespace Othello.ConsoleApp;

public interface IBoardRenderer
{
    void Render(IBoardReader board);
}
