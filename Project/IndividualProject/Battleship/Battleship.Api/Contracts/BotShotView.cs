using Battleship.Core;

namespace Battleship.Api;

/// <summary>A shot the bot fired in reply to the player.</summary>
internal sealed record BotShotView(CoordinateDto Target, ShotResult Result);
