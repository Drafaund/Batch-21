using Battleship.Core;

namespace Battleship.Api;

internal sealed record ShotResolvedEvent(string Shooter, CoordinateDto Target, ShotResult Result, GameView View);
