using Battleship.Core;

namespace Battleship.Api;

public sealed record ShotResolvedEvent(string Shooter, CoordinateDto Target, ShotResult Result, GameView View);
