using Battleship.Core;

namespace Battleship.Api;

/// <summary>
/// A human who plays through the API. Their targets arrive in requests and go to
/// <see cref="GameController.TakeTurn(Coordinate)"/>, so the controller never asks this player for a target.
/// </summary>
internal sealed class OnlinePlayer : Player, IShotStrategy
{
    public OnlinePlayer(int id, string name, IBoard board, IFleet fleet)
        : base(id, name, board, fleet)
    {
    }

    /// <exception cref="InvalidOperationException">Always; online players fire through <c>TakeTurn(Coordinate)</c>.</exception>
    public Coordinate SelectTarget()
    {
        throw new InvalidOperationException("Online players fire through GameController.TakeTurn(Coordinate).");
    }

    /// <summary>The client learns the result from the response, so there is nothing to remember here.</summary>
    public void ReceiveResult(Coordinate target, ShotResult result)
    {
    }
}
