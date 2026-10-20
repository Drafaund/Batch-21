namespace Battleship.Core;

/// <summary>A ship with its type, length, position, and damage.</summary>
public interface IShip
{
    /// <summary>Which kind of ship this is.</summary>
    ShipType Type { get; }
    /// <summary>How many cells the ship covers; it follows from <see cref="Type"/>.</summary>
    int Length { get; }
    /// <summary>The direction the ship extends from its first cell.</summary>
    Orientation Orientation { get; set; }
    /// <summary>The cells the ship covers; empty until it is placed.</summary>
    List<Coordinate> Positions { get; set; }
    /// <summary>How many cells of the ship were hit.</summary>
    int HitCount { get; set; }
    /// <summary>True when every cell of the ship was hit.</summary>
    bool IsSunk { get; }
}
