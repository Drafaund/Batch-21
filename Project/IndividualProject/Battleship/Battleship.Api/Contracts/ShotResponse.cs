using Battleship.Core;

namespace Battleship.Api;

internal sealed record ShotResponse(ShotResult Result, IReadOnlyList<BotShotView> BotShots, GameView View);
