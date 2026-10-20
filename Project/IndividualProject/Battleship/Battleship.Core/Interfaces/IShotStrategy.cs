namespace Battleship.Core;

/// <summary>Decides where a player shoots and learns from the outcome.</summary>
public interface IShotStrategy
{
    /// <summary>Chooses the next cell to fire at.</summary>
    Coordinate SelectTarget();
    /// <summary>Tells the strategy how its shot at <paramref name="target"/> turned out.</summary>
    void ReceiveResult(Coordinate target, ShotResult result);
}
