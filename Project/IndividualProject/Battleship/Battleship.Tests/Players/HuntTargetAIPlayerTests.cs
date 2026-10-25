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

    [Fact]
    public void Constructor_NullRandom_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HuntTargetAIPlayer(1, "Bot", new Board(), new Fleet(), null!));
    }

    [Fact]
    public void HuntTargetAIPlayer_IsPlayerAndShotStrategy()
    {
        var bot = NewBot(new Random(1));

        Assert.IsAssignableFrom<IPlayer>(bot);
        Assert.IsAssignableFrom<IShotStrategy>(bot);
    }

    [Fact]
    public void SelectTarget_NothingHitYet_HuntsRandomlyInsideBoard()
    {
        var bot = NewBot(new Random(4));

        var shots = Fire(bot, 40, ShotResult.Miss);

        Assert.All(shots, c => Assert.InRange(c.X, 0, 9));
        Assert.All(shots, c => Assert.InRange(c.Y, 0, 9));
    }

    [Fact]
    public void SelectTarget_AfterHit_FiresAtAllFourNeighboursOfTheHit()
    {
        var bot = NewBot(new FakeRandom(55)); // hunting picks the 56th unfired cell: (5,5)
        var hit = bot.SelectTarget();
        Assert.Equal(new Coordinate(5, 5), hit);
        bot.ReceiveResult(hit, ShotResult.Hit);

        var next = Fire(bot, 4, ShotResult.Miss);

        Assert.Equivalent(
            new[] { new Coordinate(5, 4), new Coordinate(5, 6), new Coordinate(4, 5), new Coordinate(6, 5) },
            next);
    }

    [Fact]
    public void SelectTarget_AfterMiss_KeepsHuntingInsteadOfFiringAtNeighbours()
    {
        var bot = NewBot(new FakeRandom(55, 0)); // first hunt: (5,5), second hunt: first unfired cell (0,0)
        var shot = bot.SelectTarget();
        bot.ReceiveResult(shot, ShotResult.Miss);

        var next = bot.SelectTarget();

        Assert.Equal(new Coordinate(5, 5), shot);
        Assert.Equal(new Coordinate(0, 0), next);
        Assert.False(AreNeighbours(shot, next));
    }

    [Fact]
    public void SelectTarget_HitInCorner_OnlyQueuesNeighboursInsideBoard()
    {
        var bot = NewBot(new FakeRandom(0)); // first unfired cell: (0,0)
        var hit = bot.SelectTarget();
        Assert.Equal(new Coordinate(0, 0), hit);
        bot.ReceiveResult(hit, ShotResult.Hit);

        var next = Fire(bot, 2, ShotResult.Miss);

        Assert.Equivalent(new[] { new Coordinate(0, 1), new Coordinate(1, 0) }, next);
    }

    [Fact]
    public void SelectTarget_AfterSunk_DropsQueueAndGoesBackToHunting()
    {
        var bot = NewBot(new FakeRandom(55, 0)); // hunts: (5,5), then after the sink the first unfired cell
        var hit = bot.SelectTarget(); // (5,5)
        bot.ReceiveResult(hit, ShotResult.Hit);
        var neighbour = bot.SelectTarget(); // (5,4), from the queue
        bot.ReceiveResult(neighbour, ShotResult.Sunk);

        var next = bot.SelectTarget();

        Assert.False(AreNeighbours(hit, next));
        Assert.Equal(new Coordinate(0, 0), next); // back to hunting: first unfired cell
    }

    [Fact]
    public void SelectTarget_SecondHitWhileTargeting_QueuesItsNeighboursToo()
    {
        var bot = NewBot(new FakeRandom(55));
        var first = bot.SelectTarget(); // (5,5)
        bot.ReceiveResult(first, ShotResult.Hit);
        var second = bot.SelectTarget(); // (5,4)
        bot.ReceiveResult(second, ShotResult.Hit);

        // 3 neighbours left from the first hit, plus (5,3), (4,4), (6,4) from the second hit.
        var queued = Fire(bot, 6, ShotResult.Miss);

        Assert.All(queued, t => Assert.True(
            AreNeighbours(t, first) || AreNeighbours(t, second),
            $"{t} is not next to a hit"));
        Assert.Contains(new Coordinate(5, 3), queued);
    }

    [Fact]
    public void SelectTarget_NeighbourAlreadyFired_IsNotShotAgain()
    {
        var bot = NewBot(new FakeRandom(55));
        var shots = new List<Coordinate>();
        for (var i = 0; i < 2; i++)
        {
            var target = bot.SelectTarget(); // (5,5), then (5,4); the second hit must not re-queue (5,5)
            bot.ReceiveResult(target, ShotResult.Hit);
            shots.Add(target);
        }

        shots.AddRange(Fire(bot, 30, ShotResult.Miss));

        Assert.Equal(shots.Count, shots.Distinct().Count());
    }

    [Theory]
    [InlineData(1, ShotResult.Miss)]
    [InlineData(2, ShotResult.Hit)]
    [InlineData(3, ShotResult.Hit)]
    public void SelectTarget_HundredShots_NeverRepeatsAndCoversWholeBoard(int seed, ShotResult resultToReport)
    {
        var bot = NewBot(new Random(seed));

        var shots = Fire(bot, 100, resultToReport);

        Assert.Equal(100, shots.Distinct().Count());
        Assert.All(shots, c => Assert.InRange(c.X, 0, 9));
        Assert.All(shots, c => Assert.InRange(c.Y, 0, 9));
        Assert.Throws<InvalidOperationException>(() => bot.SelectTarget());
    }

    [Fact]
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

        Assert.Equal(Play(), Play());
    }
}
