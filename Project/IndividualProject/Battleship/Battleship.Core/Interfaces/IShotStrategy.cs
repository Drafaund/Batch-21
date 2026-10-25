namespace Battleship.Core;

public interface IShotStrategy
{
    Coordinate SelectTarget();
    void ReceiveResult(Coordinate target, ShotResult result);
}
