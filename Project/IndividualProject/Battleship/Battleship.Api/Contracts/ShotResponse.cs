using Battleship.Core;

namespace Battleship.Api;

/// <summary>Answer to a shot: its result, the bot's replies, and the new view.</summary>
internal sealed record ShotResponse(ShotResult Result, IReadOnlyList<BotShotView> BotShots, GameView View);
