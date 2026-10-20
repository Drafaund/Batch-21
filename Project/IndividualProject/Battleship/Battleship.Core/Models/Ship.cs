namespace Battleship.Core;

/// <summary>A ship; its length follows from its type.</summary>
public class Ship : IShip
{
    /// <summary>Creates an unplaced, undamaged ship of the given type.</summary>
    public Ship(ShipType type)
    {
        Type = type;
        Length = type switch
        {
            ShipType.Carrier => 5,
            ShipType.Battleship => 4,
            ShipType.Cruiser => 3,
            ShipType.Submarine => 3,
            ShipType.Destroyer => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown ship type."),
        };
        Positions = new List<Coordinate>();
    }

    /// <inheritdoc />
    public ShipType Type { get; }
    /// <inheritdoc />
    public int Length { get; }
    /// <inheritdoc />
    public Orientation Orientation { get; set; }
    /// <inheritdoc />
    public List<Coordinate> Positions { get; set; }
    /// <inheritdoc />
    public int HitCount { get; set; }
    /// <inheritdoc />
    public bool IsSunk => HitCount >= Length;
}
