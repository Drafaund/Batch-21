using Battleship.Core;

namespace Battleship.Api;

/// <summary>Body of <c>POST /api/games/{code}/ships</c>.</summary>
internal sealed record PlaceShipRequest(ShipType? Type, CoordinateDto? Start, Orientation? Orientation);
