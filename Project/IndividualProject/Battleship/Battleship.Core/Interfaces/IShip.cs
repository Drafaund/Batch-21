namespace Battleship.Core;

public interface IShip
{
    ShipType Type { get; }
    int Length { get; }
    Orientation Orientation { get; set; }
    List<Coordinate> Positions { get; set; }
    int HitCount { get; set; }
    bool IsSunk { get; }
}
