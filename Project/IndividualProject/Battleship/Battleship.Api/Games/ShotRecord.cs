using Battleship.Core;

namespace Battleship.Api;

internal sealed record ShotRecord(IPlayer Shooter, Coordinate Target, ShotResult Result);
