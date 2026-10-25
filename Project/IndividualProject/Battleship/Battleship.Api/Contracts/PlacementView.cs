using Battleship.Core;

namespace Battleship.Api;

internal sealed record PlacementView(IReadOnlyList<ShipType> Placed, IReadOnlyList<ShipType> Remaining);
