using Battleship.Core;

namespace Battleship.Api;

internal sealed record PlaceShipRequest(ShipType? Type, CoordinateDto? Start, Orientation? Orientation);
