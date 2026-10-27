using Battleship.Core;

namespace Battleship.Api;

public sealed record ShipView(
    ShipType Type,
    int Length,
    Orientation Orientation,
    IReadOnlyList<CoordinateDto> Positions,
    int HitCount,
    bool IsSunk);
