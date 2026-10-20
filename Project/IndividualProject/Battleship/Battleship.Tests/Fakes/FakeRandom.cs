namespace Battleship.Tests;

/// <summary>
/// A <see cref="Random"/> whose <c>Next(int)</c> returns the scripted indexes in order (each clamped to the range);
/// the last index repeats once the script runs out.
/// </summary>
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
