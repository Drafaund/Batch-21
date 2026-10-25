using Battleship.Core;

namespace Battleship.Api;

internal sealed class OnlinePlayer : Player, IShotStrategy
{
    public OnlinePlayer(int id, string name, IBoard board, IFleet fleet)
        : base(id, name, board, fleet)
    {
    }

    public Coordinate SelectTarget()
    {
        throw new InvalidOperationException("Online players fire through GameController.TakeTurn(Coordinate).");
    }

    public void ReceiveResult(Coordinate target, ShotResult result)
    {
    }
}
