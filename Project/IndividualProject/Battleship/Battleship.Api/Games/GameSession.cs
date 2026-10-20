using System.Security.Cryptography;
using System.Text;
using Battleship.Core;

namespace Battleship.Api;

/// <summary>
/// One game of a human against a bot. Every command takes the session lock, so two requests for the same
/// game never run at the same time. The session also records the shots the controller reports.
/// </summary>
internal sealed class GameSession
{
    private readonly object _sync = new();
    private readonly GameController _controller;
    private readonly OnlinePlayer _human;
    private readonly Player _bot;
    private readonly Random _random;
    private readonly ILogger<GameSession> _logger;
    private readonly byte[] _tokenBytes;
    private readonly List<ShotRecord> _shots = new();
    private IPlayer? _winner;
    private int _version = 1;

    public GameSession(
        string code,
        string playerToken,
        GameController controller,
        OnlinePlayer human,
        Player bot,
        Random random,
        ILogger<GameSession> logger)
    {
        Code = code;
        PlayerToken = playerToken;
        _tokenBytes = Encoding.UTF8.GetBytes(playerToken);
        _controller = controller;
        _human = human;
        _bot = bot;
        _random = random;
        _logger = logger;

        _controller.OnShotResolved += HandleShotResolved;
        _controller.OnGameEnded += HandleGameEnded;
    }

    public string Code { get; }

    public string PlayerToken { get; }

    /// <summary>The five ships every player gets, one per <see cref="ShipType"/>.</summary>
    public static List<IShip> CreateStandardShips()
    {
        return Enum.GetValues<ShipType>().Select(type => (IShip)new Ship(type)).ToList();
    }

    public bool IsAuthorized(string? token)
    {
        return token != null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), _tokenBytes);
    }

    public GameView GetView()
    {
        lock (_sync)
        {
            return BuildView();
        }
    }

    public GameView PlaceShip(ShipType type, Coordinate start, Orientation orientation)
    {
        lock (_sync)
        {
            EnsurePlacing();
            if (_controller.GetFleet(_human).Ships.Any(placed => placed.Type == type))
            {
                throw ApiException.Conflict($"The {type} is already placed.");
            }

            if (!_controller.PlaceShip(_human, new Ship(type), start, orientation))
            {
                throw ApiException.Conflict("The ship does not fit there (outside the board or overlapping another ship).");
            }

            StartWhenFleetIsComplete();
            _version++;
            return BuildView();
        }
    }

    public GameView PlaceShipsRandomly()
    {
        lock (_sync)
        {
            EnsurePlacing();
            if (_controller.GetFleet(_human).Ships.Count > 0)
            {
                throw ApiException.Conflict("Random placement is only possible before the first ship is placed.");
            }

            _controller.PlaceShipsRandomly(_human, CreateStandardShips(), _random);
            StartWhenFleetIsComplete();
            _version++;
            return BuildView();
        }
    }

    public ShotResponse Fire(Coordinate target)
    {
        lock (_sync)
        {
            EnsureInProgress();
            if (!ReferenceEquals(_controller.GetCurrentPlayer(), _human))
            {
                throw ApiException.Conflict("It is not your turn.");
            }

            var firstNewShot = _shots.Count;
            var result = _controller.TakeTurn(target);
            if (result != ShotResult.Invalid)
            {
                PlayBotTurns();
                _version++;
            }

            var botShots = _shots
                .Skip(firstNewShot)
                .Where(shot => ReferenceEquals(shot.Shooter, _bot))
                .Select(shot => new BotShotView(CoordinateDto.From(shot.Target), shot.Result))
                .ToList();
            return new ShotResponse(result, botShots, BuildView());
        }
    }

    private void StartWhenFleetIsComplete()
    {
        if (_controller.GetFleet(_human).Ships.Count == Enum.GetValues<ShipType>().Length)
        {
            _controller.StartGame();
        }
    }

    private void PlayBotTurns()
    {
        while (_controller.GetState() == GameState.InProgress && ReferenceEquals(_controller.GetCurrentPlayer(), _bot))
        {
            _controller.TakeTurn();
        }
    }

    private GameView BuildView() => ViewMapper.Build(Code, _version, _controller, _human, _bot, _winner);

    private void EnsurePlacing()
    {
        if (_controller.GetState() != GameState.Setup)
        {
            throw ApiException.Conflict("Ships can only be placed before the game starts.");
        }
    }

    private void EnsureInProgress()
    {
        switch (_controller.GetState())
        {
            case GameState.Setup:
                throw ApiException.Conflict("Place all your ships before firing.");
            case GameState.Finished:
                throw ApiException.Conflict("The game is over.");
        }
    }

    private void HandleShotResolved(IPlayer shooter, Coordinate target, ShotResult result)
    {
        _shots.Add(new ShotRecord(shooter, target, result));
        _logger.LogInformation("Game {Code}: {Shooter} fired at ({X}, {Y}): {Result}", Code, shooter.Name, target.X, target.Y, result);
    }

    private void HandleGameEnded(IPlayer winner)
    {
        _winner = winner;
        _logger.LogInformation("Game {Code}: {Winner} won", Code, winner.Name);
    }
}
