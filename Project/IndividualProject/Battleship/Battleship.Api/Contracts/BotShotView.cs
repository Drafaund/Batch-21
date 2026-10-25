using Battleship.Core;

namespace Battleship.Api;

internal sealed record BotShotView(CoordinateDto Target, ShotResult Result);
