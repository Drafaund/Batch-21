using Battleship.Core;

namespace Battleship.Api;

/// <summary>Which ships the player has placed and which are still to place.</summary>
internal sealed record PlacementView(IReadOnlyList<ShipType> Placed, IReadOnlyList<ShipType> Remaining);
