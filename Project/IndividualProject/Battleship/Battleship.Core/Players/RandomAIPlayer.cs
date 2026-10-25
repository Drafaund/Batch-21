namespace Battleship.Core;

public class RandomAIPlayer : Player, IShotStrategy
{
    private readonly Random _random;
    private readonly HashSet<Coordinate> _firedShots = new();

    public RandomAIPlayer(int id, string name, IBoard board, IFleet fleet, Random random)
        : base(id, name, board, fleet)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public Coordinate SelectTarget()
    {
        var candidates = GetUnfiredCells();
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("There is no cell left to fire at.");
        }

        var target = candidates[_random.Next(candidates.Count)];
        _firedShots.Add(target);
        return target;
    }

    public void ReceiveResult(Coordinate target, ShotResult result)
    {
        _firedShots.Add(target);
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
