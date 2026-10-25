using Battleship.Core;

namespace Battleship.Api;

internal sealed record ShipView(
    ShipType Type,
    int Length,
    Orientation Orientation,
    IReadOnlyList<CoordinateDto> Positions,
    int HitCount,
    bool IsSunk);
