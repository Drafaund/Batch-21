namespace Battleship.Tests;

public class FakeRandom : Random
{
    private readonly int[] _indexes;
    private int _calls;

    public FakeRandom(params int[] indexes)
    {
        _indexes = indexes.Length > 0 ? indexes : new[] { 0 };
    }

    public override int Next(int maxValue)
    {
        var index = _indexes[Math.Min(_calls, _indexes.Length - 1)];
        _calls++;
        return Math.Min(index, maxValue - 1);
    }
}
