using Battleship.Core;

namespace Battleship.Api;

public sealed record PlaceShipRequest(ShipType? Type, CoordinateDto? Start, Orientation? Orientation);
