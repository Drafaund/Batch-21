namespace Othello.Core;

public abstract class Player
{
    protected Player(PieceColor color, string name)
    {
        Color = color;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public PieceColor Color { get; }
    public string Name { get; }
}
