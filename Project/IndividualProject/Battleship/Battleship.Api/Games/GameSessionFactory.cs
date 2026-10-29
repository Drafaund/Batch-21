using Battleship.Core;

namespace Battleship.Api;

public sealed class GameSessionFactory
{
    private readonly Func<Random> _randomFactory;
    private readonly ILogger _logger;
    private readonly IGameEventSink _sink;
    private readonly GameOptions _options;

    public GameSessionFactory(Func<Random> randomFactory, ILogger logger, IGameEventSink sink, GameOptions options)
    {
        _randomFactory = randomFactory;
        _logger = logger;
        _sink = sink;
        _options = options;
    }

    public GameSession Create(string code, string playerToken, string playerName, OpponentKind opponent)
    {
        var random = _randomFactory();
        var first = new OnlinePlayer(1, playerName, new Board(), new Fleet());
        var firstSeat = new Seat(1, first, isBot: false);
        firstSeat.Take(playerName, playerToken);

        Player second;
        IShotStrategy secondStrategy;
        switch (opponent)
        {
            case OpponentKind.Random:
                var randomBot = new RandomAIPlayer(2, "Bot Random", new Board(), new Fleet(), random);
                (second, secondStrategy) = (randomBot, randomBot);
                break;
            case OpponentKind.HuntTarget:
                var huntBot = new HuntTargetAIPlayer(2, "Bot Hunter", new Board(), new Fleet(), random);
                (second, secondStrategy) = (huntBot, huntBot);
                break;
            case OpponentKind.Human:
                var guest = new OnlinePlayer(2, string.Empty, new Board(), new Fleet());
                (second, secondStrategy) = (guest, guest);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(opponent), opponent, "Unknown opponent.");
        }

        var isBot = opponent != OpponentKind.Human;
        var controller = new GameController(first, first, second, secondStrategy);
        if (isBot)
        {
            controller.PlaceShipsRandomly(second, GameSession.CreateStandardShips(), random);
        }

        return new GameSession(
            code,
            controller,
            firstSeat,
            new Seat(2, second, isBot),
            random,
            _sink,
            _options,
            _logger);
    }
}
