namespace Battleship.Tests;

using Battleship.Core;

/// <summary>Shoots a scripted list of targets in order and records every result it receives.</summary>
public class FakeShotStrategy : IShotStrategy
{
    private readonly Queue<Coordinate> _targets;

    public FakeShotStrategy(params Coordinate[] targets)
    {
        _targets = new Queue<Coordinate>(targets);
    }

    public List<(Coordinate Target, ShotResult Result)> Received { get; } = new();

    public int SelectTargetCalls { get; private set; }

    public Coordinate SelectTarget()
    {
        SelectTargetCalls++;
        return _targets.Dequeue();
    }

    public void ReceiveResult(Coordinate target, ShotResult result)
    {
        Received.Add((target, result));
    }
}
