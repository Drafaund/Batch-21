namespace Battleship.Tests;

using Battleship.Core;

public class FakePlayer : Player
{
    public FakePlayer(int id, string name)
        : base(id, name, new Board(), new Fleet())
    {
    }
}
