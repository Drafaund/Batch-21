using Battleship.Core;

namespace Battleship.Api;

public sealed record ShotResponse(ShotResult Result, IReadOnlyList<BotShotView> BotShots, GameView View);
