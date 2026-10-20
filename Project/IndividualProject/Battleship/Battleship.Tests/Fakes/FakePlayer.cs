namespace Battleship.Tests;

using Battleship.Core;

/// <summary>Minimal concrete <see cref="Player"/> for tests; has its own board and fleet.</summary>
public class FakePlayer : Player
{
    public FakePlayer(int id, string name)
        : base(id, name, new Board(), new Fleet())
    {
    }
}
