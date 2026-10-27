using Battleship.Core;

namespace Battleship.Api;

public sealed record BotShotView(CoordinateDto Target, ShotResult Result);
