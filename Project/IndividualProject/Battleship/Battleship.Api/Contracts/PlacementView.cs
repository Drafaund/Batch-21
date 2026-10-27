using Battleship.Core;

namespace Battleship.Api;

public sealed record PlacementView(IReadOnlyList<ShipType> Placed, IReadOnlyList<ShipType> Remaining);
