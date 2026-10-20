namespace Battleship.Core;

/// <summary>Decides where a player shoots and learns from the outcome.</summary>
public interface IShotStrategy
{
    Coordinate SelectTarget();
    void ReceiveResult(Coordinate target, ShotResult result);
}
