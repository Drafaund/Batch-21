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
            Assert.True(turns < MaxTurns, "The game did not finish.");
            controller.TakeTurn();
            turns++;
        }

        Assert.NotNull(announced);
        return (announced, turns);
    }

    private static void AssertFinishedWithWinner(GameController controller, IPlayer winner)
    {
        var loser = ReferenceEquals(winner, controller.Player1) ? controller.Player2 : controller.Player1;

        Assert.Equal(GameState.Finished, controller.GetState());
        Assert.Same(winner, controller.CheckWinner());
        Assert.Equal(0, controller.GetRemainingShips(loser));
        Assert.True(controller.GetRemainingShips(winner) > 0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void RandomVsRandom_PlayedToTheEnd_ProducesWinner(int seed)
    {
        var first = new RandomAIPlayer(1, "R1", new Board(), new Fleet(), new Random(seed));
        var second = new RandomAIPlayer(2, "R2", new Board(), new Fleet(), new Random(seed + 100));
        var controller = NewGame(first, second, seed);

        var (winner, _) = PlayToEnd(controller);

        AssertFinishedWithWinner(controller, winner);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void HuntTargetVsHuntTarget_PlayedToTheEnd_ProducesWinner(int seed)
    {
        var first = new HuntTargetAIPlayer(1, "H1", new Board(), new Fleet(), new Random(seed));
        var second = new HuntTargetAIPlayer(2, "H2", new Board(), new Fleet(), new Random(seed + 100));
        var controller = NewGame(first, second, seed);

        var (winner, _) = PlayToEnd(controller);

        AssertFinishedWithWinner(controller, winner);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RandomVsHuntTarget_PlayedToTheEnd_ProducesWinner(int seed)
    {
        var first = new RandomAIPlayer(1, "R", new Board(), new Fleet(), new Random(seed));
        var second = new HuntTargetAIPlayer(2, "H", new Board(), new Fleet(), new Random(seed + 100));
        var controller = NewGame(first, second, seed);

        var (winner, _) = PlayToEnd(controller);

        AssertFinishedWithWinner(controller, winner);
    }

    [Fact]
    public void AiVsAi_FullGame_RaisesShotEventForEveryTurnAndEndsOnce()
    {
        var first = new HuntTargetAIPlayer(1, "H1", new Board(), new Fleet(), new Random(8));
        var second = new RandomAIPlayer(2, "R2", new Board(), new Fleet(), new Random(9));
        var controller = NewGame(first, second, 8);
        var shots = 0;
        var ended = 0;
        controller.OnShotResolved += (_, _, result) =>
        {
            Assert.NotEqual(ShotResult.Invalid, result);
            shots++;
        };
        controller.OnGameEnded += _ => ended++;

        var turns = 0;
        while (controller.GetState() == GameState.InProgress)
        {
            controller.TakeTurn();
            turns++;
        }

        Assert.Equal(turns, shots);
        Assert.Equal(1, ended);
    }

    [Fact]
    public void AiVsAi_SameSeeds_PlayTheSameGame()
    {
        (string Winner, int Turns) Play()
        {
            var first = new HuntTargetAIPlayer(1, "H", new Board(), new Fleet(), new Random(21));
            var second = new RandomAIPlayer(2, "R", new Board(), new Fleet(), new Random(22));
            var (winner, turns) = PlayToEnd(NewGame(first, second, 21));
            return (winner.Name, turns);
        }

        Assert.Equal(Play(), Play());
    }

    [Fact]
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

        Assert.True(hunt < random, $"HuntTarget averaged {hunt:F1} shots, Random averaged {random:F1}.");
    }
}
