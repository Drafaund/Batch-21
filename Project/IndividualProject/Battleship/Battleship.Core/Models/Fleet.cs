namespace Battleship.Core;

/// <summary>The ships of one player.</summary>
public class Fleet : IFleet
{
    /// <summary>Creates a fleet with no ships.</summary>
    public Fleet()
    {
        Ships = new List<IShip>();
    }

    /// <inheritdoc />
    public List<IShip> Ships { get; set; }
}
