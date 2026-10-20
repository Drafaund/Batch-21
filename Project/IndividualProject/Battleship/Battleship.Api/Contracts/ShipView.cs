using Battleship.Core;

namespace Battleship.Api;

/// <summary>One of the player's own ships.</summary>
internal sealed record ShipView(
    ShipType Type,
    int Length,
    Orientation Orientation,
    IReadOnlyList<CoordinateDto> Positions,
    int HitCount,
    bool IsSunk);
