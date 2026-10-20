namespace Battleship.Core;

/// <summary>Base class that holds the identity, board, and fleet of a player. Shooting is left to the concrete player types, which implement <see cref="IShotStrategy"/>.</summary>
public abstract class Player : IPlayer
{
    /// <summary>Sets the identity, board, and fleet.</summary>
    protected Player(int id, string name, IBoard board, IFleet fleet)
    {
        Id = id;
        Name = name;
        Board = board;
        Fleet = fleet;
    }

    /// <inheritdoc />
    public int Id { get; }
    /// <inheritdoc />
    public string Name { get; set; }
    /// <inheritdoc />
    public IBoard Board { get; set; }
    /// <inheritdoc />
    public IFleet Fleet { get; set; }
}
