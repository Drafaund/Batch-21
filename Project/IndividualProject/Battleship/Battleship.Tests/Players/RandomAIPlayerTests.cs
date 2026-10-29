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

    [Test]
    public void Constructor_NullRandom_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RandomAIPlayer(1, "Bot", new Board(), new Fleet(), null!));
    }

    [Test]
    public void RandomAIPlayer_IsPlayerAndShotStrategy()
    {
        var bot = NewBot(1);

        Assert.That(bot, Is.AssignableTo<IPlayer>());
        Assert.That(bot, Is.AssignableTo<IShotStrategy>());
    }

    [TestCase(1)]
    [TestCase(42)]
    [TestCase(2026)]
    public void SelectTarget_HundredShots_CoversWholeBoardWithoutRepeats(int seed)
    {
        var bot = NewBot(seed);

        var shots = FireAll(bot, 100);

        Assert.That(shots.Distinct().Count(), Is.EqualTo(100));
        Assert.That(shots.Select(c => c.X), Is.All.InRange(0, 9));
        Assert.That(shots.Select(c => c.Y), Is.All.InRange(0, 9));
    }

    [Test]
    public void SelectTarget_NoResultReported_StillNeverRepeats()
    {
        var bot = NewBot(5);

        var shots = Enumerable.Range(0, 100).Select(_ => bot.SelectTarget()).ToList();

        Assert.That(shots.Distinct().Count(), Is.EqualTo(100));
    }

    [Test]
    public void SelectTarget_AfterWholeBoardFired_Throws()
    {
        var bot = NewBot(3);
        FireAll(bot, 100);

        Assert.Throws<InvalidOperationException>(() => bot.SelectTarget());
    }

    [Test]
    public void SelectTarget_SameSeed_GivesSameSequence()
    {
        var first = FireAll(NewBot(77), 30);
        var second = FireAll(NewBot(77), 30);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void SelectTarget_DifferentSeeds_GiveDifferentSequences()
    {
        var first = FireAll(NewBot(1), 30);
        var second = FireAll(NewBot(2), 30);

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
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

        Assert.That(withHits, Is.EqualTo(withMisses));
    }
}
