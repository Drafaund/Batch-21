namespace Battleship.Core;

public class Ship : IShip
{
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

    public ShipType Type { get; }
    public int Length { get; }
    public Orientation Orientation { get; set; }
    public List<Coordinate> Positions { get; set; }
    public int HitCount { get; set; }
    public bool IsSunk => HitCount >= Length;
}
