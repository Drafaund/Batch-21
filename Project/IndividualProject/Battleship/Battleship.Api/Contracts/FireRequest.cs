namespace Battleship.Api;

/// <summary>Body of <c>POST /api/games/{code}/shots</c>.</summary>
internal sealed record FireRequest(CoordinateDto? Target);
