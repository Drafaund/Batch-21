namespace Othello.Core;

public abstract class Player : IPlayer
{
    protected Player(DiscColor color, string name)
    {
        if (color == DiscColor.Empty) throw new ArgumentException("Player color cannot be Empty.", nameof(color));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Player name is required.", nameof(name));
        Color = color;
        Name = name;
    }
    public DiscColor Color { get; }
    public string Name { get; }
}
