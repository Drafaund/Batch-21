namespace Battleship.Core;

public class HuntTargetAIPlayer : Player, IShotStrategy
{
    private static readonly (int Dx, int Dy)[] NeighbourOffsets = { (0, -1), (0, 1), (-1, 0), (1, 0) };

    private readonly Random _random;
    private readonly HashSet<Coordinate> _firedShots = new();
    private readonly Queue<Coordinate> _targetQueue = new();

    public HuntTargetAIPlayer(int id, string name, IBoard board, IFleet fleet, Random random)
        : base(id, name, board, fleet)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public Coordinate SelectTarget()
    {
        var target = DequeueTarget() ?? PickRandomUnfiredCell();
        _firedShots.Add(target);
        return target;
    }

    public void ReceiveResult(Coordinate target, ShotResult result)
    {
        _firedShots.Add(target);

        switch (result)
        {
            case ShotResult.Hit:
                EnqueueNeighbours(target);
                break;
            case ShotResult.Sunk:
                _targetQueue.Clear();
                break;
        }
    }

    private Coordinate? DequeueTarget()
    {
        while (_targetQueue.Count > 0)
        {
            var candidate = _targetQueue.Dequeue();
            if (!_firedShots.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private void EnqueueNeighbours(Coordinate hit)
    {
        foreach (var (dx, dy) in NeighbourOffsets)
        {
            var neighbour = new Coordinate(hit.X + dx, hit.Y + dy);
            if (IsInsideBoard(neighbour) && !_firedShots.Contains(neighbour))
            {
                _targetQueue.Enqueue(neighbour);
            }
        }
    }

    private Coordinate PickRandomUnfiredCell()
    {
        var candidates = GetUnfiredCells();
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("There is no cell left to fire at.");
        }

        return candidates[_random.Next(candidates.Count)];
    }

    private bool IsInsideBoard(Coordinate position)
    {
        return position.X >= 0 && position.X < Board.BoardSize
            && position.Y >= 0 && position.Y < Board.BoardSize;
    }

    private List<Coordinate> GetUnfiredCells()
    {
        var cells = new List<Coordinate>();
        for (var y = 0; y < Board.BoardSize; y++)
        {
            for (var x = 0; x < Board.BoardSize; x++)
            {
                var cell = new Coordinate(x, y);
                if (!_firedShots.Contains(cell))
                {
                    cells.Add(cell);
                }
            }
        }

        return cells;
    }
}
