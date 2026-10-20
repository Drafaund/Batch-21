using Battleship.Core;

namespace Battleship.Api;

/// <summary>Composition root for one game: creates the players, boards, fleets, and the controller.</summary>
internal sealed class GameSessionFactory
{
    private readonly Func<Random> _randomFactory;
    private readonly ILoggerFactory _loggerFactory;

    public GameSessionFactory(Func<Random> randomFactory, ILoggerFactory loggerFactory)
    {
        _randomFactory = randomFactory;
        _loggerFactory = loggerFactory;
    }

    /// <summary>Creates a game for one human against a bot; the bot's ships are placed straight away.</summary>
    public GameSession Create(string code, string playerToken, string playerName, OpponentKind opponent)
    {
        var random = _randomFactory();
        var human = new OnlinePlayer(1, playerName, new Board(), new Fleet());
        Player bot = opponent switch
        {
            OpponentKind.Random => new RandomAIPlayer(2, "Bot Random", new Board(), new Fleet(), random),
            OpponentKind.HuntTarget => new HuntTargetAIPlayer(2, "Bot Hunter", new Board(), new Fleet(), random),
            _ => throw new ArgumentOutOfRangeException(nameof(opponent), opponent, "Only bot opponents are supported."),
        };

        var controller = new GameController(human, human, bot, (IShotStrategy)bot);
        controller.PlaceShipsRandomly(bot, GameSession.CreateStandardShips(), random);

        return new GameSession(code, playerToken, controller, human, bot, random, _loggerFactory.CreateLogger<GameSession>());
    }
}
