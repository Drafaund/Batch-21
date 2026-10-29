namespace Battleship.Tests;

using Battleship.Core;

public class AiGameTests
{
    private const int MaxTurns = 400;

    private static IShip[] NewShips() => TestGame.AllTypes.Select(type => (IShip)new Ship(type)).ToArray();

    private static GameController NewGame<T1, T2>(T1 first, T2 second, int seed)
        where T1 : IPlayer, IShotStrategy
        where T2 : IPlayer, IShotStrategy
    {
        var controller = new GameController(first, first, second, second);
        controller.PlaceShipsRandomly(first, NewShips(), new Random(seed));
        controller.PlaceShipsRandomly(second, NewShips(), new Random(seed + 1000));
        controller.StartGame();
        return controller;
    }

    private static (IPlayer Winner, int Turns) PlayToEnd(GameController controller)
    {
        IPlayer? announced = null;
        controller.OnGameEnded += winner => announced = winner;

        var turns = 0;
        while (controller.GetState() == GameState.InProgress)
        {
            Assert.That(turns < MaxTurns, Is.True, "The game did not finish.");
            controller.TakeTurn();
            turns++;
        }

        Assert.That(announced, Is.Not.Null);
        return (announced!, turns);
    }

    private static void AssertFinishedWithWinner(GameController controller, IPlayer winner)
    {
        var loser = ReferenceEquals(winner, controller.Player1) ? controller.Player2 : controller.Player1;

        Assert.That(controller.GetState(), Is.EqualTo(GameState.Finished));
        Assert.That(controller.CheckWinner(), Is.SameAs(winner));
        Assert.That(controller.GetRemainingShips(loser), Is.EqualTo(0));
        Assert.That(controller.GetRemainingShips(winner) > 0, Is.True);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void RandomVsRandom_PlayedToTheEnd_ProducesWinner(int seed)
    {
        var first = new RandomAIPlayer(1, "R1", new Board(), new Fleet(), new Random(seed));
        var second = new RandomAIPlayer(2, "R2", new Board(), new Fleet(), new Random(seed + 100));
        var controller = NewGame(first, second, seed);

        var (winner, _) = PlayToEnd(controller);

        AssertFinishedWithWinner(controller, winner);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void HuntTargetVsHuntTarget_PlayedToTheEnd_ProducesWinner(int seed)
    {
        var first = new HuntTargetAIPlayer(1, "H1", new Board(), new Fleet(), new Random(seed));
        var second = new HuntTargetAIPlayer(2, "H2", new Board(), new Fleet(), new Random(seed + 100));
        var controller = NewGame(first, second, seed);

        var (winner, _) = PlayToEnd(controller);

        AssertFinishedWithWinner(controller, winner);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void RandomVsHuntTarget_PlayedToTheEnd_ProducesWinner(int seed)
    {
        var first = new RandomAIPlayer(1, "R", new Board(), new Fleet(), new Random(seed));
        var second = new HuntTargetAIPlayer(2, "H", new Board(), new Fleet(), new Random(seed + 100));
        var controller = NewGame(first, second, seed);

        var (winner, _) = PlayToEnd(controller);

        AssertFinishedWithWinner(controller, winner);
    }

    [Test]
    public void AiVsAi_FullGame_RaisesShotEventForEveryTurnAndEndsOnce()
    {
        var first = new HuntTargetAIPlayer(1, "H1", new Board(), new Fleet(), new Random(8));
        var second = new RandomAIPlayer(2, "R2", new Board(), new Fleet(), new Random(9));
        var controller = NewGame(first, second, 8);
        var shots = 0;
        var ended = 0;
        controller.OnShotResolved += (_, _, result) =>
        {
            Assert.That(result, Is.Not.EqualTo(ShotResult.Invalid));
            shots++;
        };
        controller.OnGameEnded += _ => ended++;

        var turns = 0;
        while (controller.GetState() == GameState.InProgress)
        {
            controller.TakeTurn();
            turns++;
        }

        Assert.That(shots, Is.EqualTo(turns));
        Assert.That(ended, Is.EqualTo(1));
    }

    [Test]
    public void AiVsAi_SameSeeds_PlayTheSameGame()
    {
        (string Winner, int Turns) Play()
        {
            var first = new HuntTargetAIPlayer(1, "H", new Board(), new Fleet(), new Random(21));
            var second = new RandomAIPlayer(2, "R", new Board(), new Fleet(), new Random(22));
            var (winner, turns) = PlayToEnd(NewGame(first, second, 21));
            return (winner.Name, turns);
        }

        Assert.That(Play(), Is.EqualTo(Play()));
    }

    [Test]
    public void ShotsNeededToSinkFleet_HuntTargetNeedsFewerOnAverageThanRandom()
    {
        const int games = 100;

        double Average(Func<Random, IShotStrategy> createBot)
        {
            var total = 0;
            for (var seed = 0; seed < games; seed++)
            {
                var shooter = new FakePlayer(1, "Shooter");
                var target = new FakePlayer(2, "Target");
                var bot = createBot(new Random(seed));
                var controller = new GameController(shooter, bot, target, new FakeShotStrategy());
                controller.PlaceShipsRandomly(shooter, NewShips(), new Random(seed));
                controller.PlaceShipsRandomly(target, NewShips(), new Random(seed + 1000));
                controller.StartGame();

                while (controller.CheckWinner() == null)
                {
                    var cell = bot.SelectTarget();
                    bot.ReceiveResult(cell, controller.ProcessShot(shooter, cell));
                    total++;
                }
            }

            return (double)total / games;
        }

        var random = Average(r => new RandomAIPlayer(1, "R", new Board(), new Fleet(), r));
        var hunt = Average(r => new HuntTargetAIPlayer(1, "H", new Board(), new Fleet(), r));

        Assert.That(hunt < random, Is.True, $"HuntTarget averaged {hunt:F1} shots, Random averaged {random:F1}.");
    }
}
