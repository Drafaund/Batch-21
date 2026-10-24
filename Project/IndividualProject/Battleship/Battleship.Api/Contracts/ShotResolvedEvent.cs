using Battleship.Core;

namespace Battleship.Api;

/// <summary>Sent to both players after every valid shot. <paramref name="Shooter"/> is "you" or "opponent" for the one who receives it.</summary>
internal sealed record ShotResolvedEvent(string Shooter, CoordinateDto Target, ShotResult Result, GameView View);
