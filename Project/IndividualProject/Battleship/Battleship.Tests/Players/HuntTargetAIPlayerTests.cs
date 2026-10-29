namespace Battleship.Tests;

using Battleship.Core;

public class HuntTargetAIPlayerTests
{
    private static HuntTargetAIPlayer NewBot(Random random) => new(1, "Bot", new Board(), new Fleet(), random);

    private static bool AreNeighbours(Coordinate a, Coordinate b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;

    private static List<Coordinate> Fire(HuntTargetAIPlayer bot, int count, ShotResult result)
    {
        var shots = new List<Coordinate>();
        for (var i = 0; i < count; i++)
        {
            var target = bot.SelectTarget();
            bot.ReceiveResult(target, result);
            shots.Add(target);
        }

        return shots;
    }

    [Test]
    public void Constructor_NullRandom_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HuntTargetAIPlayer(1, "Bot", new Board(), new Fleet(), null!));
    }

    [Test]
    public void HuntTargetAIPlayer_IsPlayerAndShotStrategy()
    {
        var bot = NewBot(new Random(1));

        Assert.That(bot, Is.AssignableTo<IPlayer>());
        Assert.That(bot, Is.AssignableTo<IShotStrategy>());
    }

    [Test]
    public void SelectTarget_NothingHitYet_HuntsRandomlyInsideBoard()
    {
        var bot = NewBot(new Random(4));

        var shots = Fire(bot, 40, ShotResult.Miss);

        Assert.That(shots.Select(c => c.X), Is.All.InRange(0, 9));
        Assert.That(shots.Select(c => c.Y), Is.All.InRange(0, 9));
    }

    [Test]
    public void SelectTarget_AfterHit_FiresAtAllFourNeighboursOfTheHit()
    {
        var bot = NewBot(new FakeRandom(55));
        var hit = bot.SelectTarget();
        Assert.That(hit, Is.EqualTo(new Coordinate(5, 5)));
        bot.ReceiveResult(hit, ShotResult.Hit);

        var next = Fire(bot, 4, ShotResult.Miss);

        Assert.That(next, Is.EquivalentTo(new[] { new Coordinate(5, 4), new Coordinate(5, 6), new Coordinate(4, 5), new Coordinate(6, 5) }));
    }

    [Test]
    public void SelectTarget_AfterMiss_KeepsHuntingInsteadOfFiringAtNeighbours()
    {
        var bot = NewBot(new FakeRandom(55, 0));
        var shot = bot.SelectTarget();
        bot.ReceiveResult(shot, ShotResult.Miss);

        var next = bot.SelectTarget();

        Assert.That(shot, Is.EqualTo(new Coordinate(5, 5)));
        Assert.That(next, Is.EqualTo(new Coordinate(0, 0)));
        Assert.That(AreNeighbours(shot, next), Is.False);
    }

    [Test]
    public void SelectTarget_HitInCorner_OnlyQueuesNeighboursInsideBoard()
    {
        var bot = NewBot(new FakeRandom(0));
        var hit = bot.SelectTarget();
        Assert.That(hit, Is.EqualTo(new Coordinate(0, 0)));
        bot.ReceiveResult(hit, ShotResult.Hit);

        var next = Fire(bot, 2, ShotResult.Miss);

        Assert.That(next, Is.EquivalentTo(new[] { new Coordinate(0, 1), new Coordinate(1, 0) }));
    }

    [Test]
    public void SelectTarget_AfterSunk_DropsQueueAndGoesBackToHunting()
    {
        var bot = NewBot(new FakeRandom(55, 0));
        var hit = bot.SelectTarget();
        bot.ReceiveResult(hit, ShotResult.Hit);
        var neighbour = bot.SelectTarget();
        bot.ReceiveResult(neighbour, ShotResult.Sunk);

        var next = bot.SelectTarget();

        Assert.That(AreNeighbours(hit, next), Is.False);
        Assert.That(next, Is.EqualTo(new Coordinate(0, 0)));
    }

    [Test]
    public void SelectTarget_SecondHitWhileTargeting_QueuesItsNeighboursToo()
    {
        var bot = NewBot(new FakeRandom(55));
        var first = bot.SelectTarget();
        bot.ReceiveResult(first, ShotResult.Hit);
        var second = bot.SelectTarget();
        bot.ReceiveResult(second, ShotResult.Hit);

        var queued = Fire(bot, 6, ShotResult.Miss);

        Assert.That(queued, Has.All.Matches<Coordinate>(t => AreNeighbours(t, first) || AreNeighbours(t, second)), "Every queued shot should be next to a hit.");
        Assert.That(queued, Does.Contain(new Coordinate(5, 3)));
    }

    [Test]
    public void SelectTarget_NeighbourAlreadyFired_IsNotShotAgain()
    {
        var bot = NewBot(new FakeRandom(55));
        var shots = new List<Coordinate>();
        for (var i = 0; i < 2; i++)
        {
            var target = bot.SelectTarget();
            bot.ReceiveResult(target, ShotResult.Hit);
            shots.Add(target);
        }

        shots.AddRange(Fire(bot, 30, ShotResult.Miss));

        Assert.That(shots.Distinct().Count(), Is.EqualTo(shots.Count));
    }

    [TestCase(1, ShotResult.Miss)]
    [TestCase(2, ShotResult.Hit)]
    [TestCase(3, ShotResult.Hit)]
    public void SelectTarget_HundredShots_NeverRepeatsAndCoversWholeBoard(int seed, ShotResult resultToReport)
    {
        var bot = NewBot(new Random(seed));

        var shots = Fire(bot, 100, resultToReport);

        Assert.That(shots.Distinct().Count(), Is.EqualTo(100));
        Assert.That(shots.Select(c => c.X), Is.All.InRange(0, 9));
        Assert.That(shots.Select(c => c.Y), Is.All.InRange(0, 9));
        Assert.Throws<InvalidOperationException>(() => bot.SelectTarget());
    }

    [Test]
    public void SelectTarget_SameSeedAndResults_GiveSameSequence()
    {
        List<Coordinate> Play()
        {
            var bot = NewBot(new Random(11));
            var shots = new List<Coordinate>();
            for (var i = 0; i < 40; i++)
            {
                var target = bot.SelectTarget();
                shots.Add(target);
                bot.ReceiveResult(target, i % 3 == 0 ? ShotResult.Hit : ShotResult.Miss);
            }

            return shots;
        }

        Assert.That(Play(), Is.EqualTo(Play()));
    }
}
