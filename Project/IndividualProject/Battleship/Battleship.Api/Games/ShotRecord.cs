using Battleship.Core;

namespace Battleship.Api;

/// <summary>A shot as the controller reported it, kept so the bot's replies can be returned to the player.</summary>
internal sealed record ShotRecord(IPlayer Shooter, Coordinate Target, ShotResult Result);
