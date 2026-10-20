namespace Battleship.Tests;

using Battleship.Core;

public class RandomAIPlayerTests
{
    private static RandomAIPlayer NewBot(int seed) => new(1, "Bot", new Board(), new Fleet(), new Random(seed));

    private static List<Coordinate> FireAll(RandomAIPlayer bot, int count)
    {
        var shots = new List<Coordinate>();
        for (var i = 0; i < count; i++)
        {
            var target = bot.SelectTarget();
            shots.Add(target);
            bot.ReceiveResult(target, ShotResult.Miss);
        }

        return shots;
    }

    [Fact]
    public void Constructor_NullRandom_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RandomAIPlayer(1, "Bot", new Board(), new Fleet(), null!));
    }

    [Fact]
    public void RandomAIPlayer_IsPlayerAndShotStrategy()
    {
        var bot = NewBot(1);

        Assert.IsAssignableFrom<IPlayer>(bot);
        Assert.IsAssignableFrom<IShotStrategy>(bot);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(2026)]
    public void SelectTarget_HundredShots_CoversWholeBoardWithoutRepeats(int seed)
    {
        var bot = NewBot(seed);

        var shots = FireAll(bot, 100);

        Assert.Equal(100, shots.Distinct().Count());
        Assert.All(shots, c => Assert.InRange(c.X, 0, 9));
        Assert.All(shots, c => Assert.InRange(c.Y, 0, 9));
    }

    [Fact]
    public void SelectTarget_NoResultReported_StillNeverRepeats()
    {
        var bot = NewBot(5);

        var shots = Enumerable.Range(0, 100).Select(_ => bot.SelectTarget()).ToList();

        Assert.Equal(100, shots.Distinct().Count());
    }

    [Fact]
    public void SelectTarget_AfterWholeBoardFired_Throws()
    {
        var bot = NewBot(3);
        FireAll(bot, 100);

        Assert.Throws<InvalidOperationException>(() => bot.SelectTarget());
    }

    [Fact]
    public void SelectTarget_SameSeed_GivesSameSequence()
    {
        var first = FireAll(NewBot(77), 30);
        var second = FireAll(NewBot(77), 30);

        Assert.Equal(first, second);
    }

    [Fact]
    public void SelectTarget_DifferentSeeds_GiveDifferentSequences()
    {
        var first = FireAll(NewBot(1), 30);
        var second = FireAll(NewBot(2), 30);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void SelectTarget_ReportedResultsDoNotChangeChoices()
    {
        var reportsMiss = NewBot(9);
        var reportsHit = NewBot(9);
        var withMisses = new List<Coordinate>();
        var withHits = new List<Coordinate>();

        for (var i = 0; i < 20; i++)
        {
            var missTarget = reportsMiss.SelectTarget();
            reportsMiss.ReceiveResult(missTarget, ShotResult.Miss);
            withMisses.Add(missTarget);

            var hitTarget = reportsHit.SelectTarget();
            reportsHit.ReceiveResult(hitTarget, ShotResult.Hit);
            withHits.Add(hitTarget);
        }

        Assert.Equal(withMisses, withHits);
    }
}
