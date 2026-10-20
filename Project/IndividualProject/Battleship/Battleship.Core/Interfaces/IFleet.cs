namespace Battleship.Core;

/// <summary>The ships of one player.</summary>
public interface IFleet
{
    /// <summary>The ships in the fleet.</summary>
    List<IShip> Ships { get; set; }
}
