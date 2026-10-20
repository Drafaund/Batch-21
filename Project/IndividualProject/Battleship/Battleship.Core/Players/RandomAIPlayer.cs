namespace Battleship.Core;

/// <summary>A bot that shoots at a random cell it has not fired at yet. It never looks at the enemy board.</summary>
public class RandomAIPlayer : Player, IShotStrategy
{
    private readonly Random _random;
    private readonly HashSet<Coordinate> _firedShots = new();

    /// <summary>Creates a bot that picks targets with <paramref name="random"/>; pass a seeded <see cref="Random"/> for repeatable games.</summary>
    public RandomAIPlayer(int id, string name, IBoard board, IFleet fleet, Random random)
        : base(id, name, board, fleet)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    /// <summary>Picks a random cell inside the board that was not fired at before.</summary>
    /// <exception cref="InvalidOperationException">Every cell has already been fired at.</exception>
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

    /// <summary>Remembers the shot; the bot keeps no other knowledge.</summary>
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
