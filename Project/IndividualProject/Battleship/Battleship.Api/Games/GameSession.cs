using Battleship.Core;

namespace Battleship.Api;

internal sealed class GameSession
{
    private readonly object _sync = new();
    private readonly GameController _controller;
    private readonly Seat[] _seats;
    private readonly Random _random;
    private readonly IGameEventSink _sink;
    private readonly GameOptions _options;
    private readonly ILogger<GameSession> _logger;
    private readonly List<ShotRecord> _shots = new();
    private IPlayer? _winner;
    private string? _endReason;
    private Timer? _turnTimer;
    private int _turnId;
    private DateTime? _turnDeadline;
    private int _version = 1;

    public GameSession(
        string code,
        GameController controller,
        Seat seat1,
        Seat seat2,
        Random random,
        IGameEventSink sink,
        GameOptions options,
        ILogger<GameSession> logger)
    {
        Code = code;
        _controller = controller;
        _seats = new[] { seat1, seat2 };
        _random = random;
        _sink = sink;
        _options = options;
        _logger = logger;

        _controller.OnShotResolved += HandleShotResolved;
        _controller.OnGameEnded += HandleGameEnded;
    }

    public string Code { get; }

    public string PlayerToken => _seats[0].Token!;

    private bool IsOnline => !_seats[1].IsBot;

    public static List<IShip> CreateStandardShips()
    {
        return Enum.GetValues<ShipType>().Select(type => (IShip)new Ship(type)).ToList();
    }

    public int? FindSeat(string? token)
    {
        return _seats.FirstOrDefault(seat => seat.Authorizes(token))?.Number;
    }

    public GameView GetView(int seat)
    {
        lock (_sync)
        {
            return BuildView(seat);
        }
    }

    public JoinGameResponse Join(string playerName, string token)
    {
        return Execute(events =>
        {
            var guest = _seats[1];
            if (!IsOnline || guest.IsTaken)
            {
                throw ApiException.Conflict("This room is already full.");
            }

            guest.Take(playerName, token);
            _version++;
            _logger.LogInformation("Game {Code}: {Name} joined", Code, playerName);
            events.Add(new SeatEvent(1, GameEventNames.PlayerJoined, new PlayerJoinedEvent(playerName, BuildView(1))));
            return new JoinGameResponse(token, 2, BuildView(2));
        });
    }

    public GameView PlaceShip(int seat, ShipType type, Coordinate start, Orientation orientation)
    {
        return Execute(events =>
        {
            EnsurePlacing();
            var player = _seats[seat - 1].Player;
            if (_controller.GetFleet(player).Ships.Any(placed => placed.Type == type))
            {
                throw ApiException.Conflict($"The {type} is already placed.");
            }

            if (!_controller.PlaceShip(player, new Ship(type), start, orientation))
            {
                throw ApiException.Conflict("The ship does not fit there (outside the board or overlapping another ship).");
            }

            StartWhenFleetsAreComplete(events);
            _version++;
            return BuildView(seat);
        });
    }

    public GameView PlaceShipsRandomly(int seat)
    {
        return Execute(events =>
        {
            EnsurePlacing();
            var player = _seats[seat - 1].Player;
            if (_controller.GetFleet(player).Ships.Count > 0)
            {
                throw ApiException.Conflict("Random placement is only possible before the first ship is placed.");
            }

            _controller.PlaceShipsRandomly(player, CreateStandardShips(), _random);
            StartWhenFleetsAreComplete(events);
            _version++;
            return BuildView(seat);
        });
    }

    public ShotResponse Fire(int seat, Coordinate target)
    {
        return Execute(events =>
        {
            EnsureInProgress();
            var shooter = _seats[seat - 1];
            if (!ReferenceEquals(_controller.GetCurrentPlayer(), shooter.Player))
            {
                throw ApiException.Conflict("It is not your turn.");
            }

            var firstNewShot = _shots.Count;
            var result = _controller.TakeTurn(target);
            if (result != ShotResult.Invalid)
            {
                PlayBotTurns();
                _version++;
                StartTurnClock();
                AddShotEvents(events, firstNewShot);
            }

            var botShots = _shots
                .Skip(firstNewShot)
                .Where(shot => _seats.Any(other => other.IsBot && ReferenceEquals(shot.Shooter, other.Player)))
                .Select(shot => new BotShotView(CoordinateDto.From(shot.Target), shot.Result))
                .ToList();
            return new ShotResponse(result, botShots, BuildView(seat));
        });
    }

    public void SendEmote(int seat, string emote)
    {
        Execute(events =>
        {
            var sender = _seats[seat - 1];
            var receiver = OtherSeat(sender);
            if (!IsOnline || !receiver.IsTaken)
            {
                throw ApiException.Conflict(IsOnline
                    ? "Wait for the other player to join before sending emotes."
                    : "Emotes are only for games between two players.");
            }

            var now = DateTime.UtcNow;
            if (sender.LastEmoteAt is { } last && now - last < _options.EmoteCooldown)
            {
                throw ApiException.TooManyRequests("You are sending emotes too fast. Wait a moment.");
            }

            sender.LastEmoteAt = now;
            events.Add(new SeatEvent(receiver.Number, GameEventNames.EmoteReceived, new EmoteReceivedEvent(emote)));
        });
    }

    public void Connect(int seat)
    {
        Execute(events =>
        {
            var current = _seats[seat - 1];
            current.Connections++;
            current.StopDropTimer();
            if (current.Connections == 1 && IsOnline)
            {
                _version++;
                AddConnectionEvent(events, current, connected: true);
            }
        });
    }

    public void Disconnect(int seat)
    {
        Execute(events =>
        {
            var current = _seats[seat - 1];
            current.Connections = Math.Max(0, current.Connections - 1);
            if (current.Connections > 0 || !IsOnline)
            {
                return;
            }

            _version++;
            AddConnectionEvent(events, current, connected: false);
            if (_controller.GetState() == GameState.InProgress)
            {
                current.StopDropTimer();
                current.DropTimer = new Timer(_ => Forfeit(current), null, _options.OpponentTimeout, Timeout.InfiniteTimeSpan);
            }
        });
    }

    private void Forfeit(Seat absent)
    {
        Execute(events =>
        {
            if (absent.IsConnected || _controller.GetState() != GameState.InProgress)
            {
                return;
            }

            var winner = OtherSeat(absent).Player;
            _endReason = GameEndReasons.Forfeit;
            _logger.LogInformation("Game {Code}: {Name} stayed away and forfeits", Code, absent.Player.Name);
            _controller.EndGame(winner);
            _version++;
            AddEndEvent(events);
        });
    }

    private void StartWhenFleetsAreComplete(List<SeatEvent> events)
    {
        var shipCount = Enum.GetValues<ShipType>().Length;
        if (_seats.Any(seat => _controller.GetFleet(seat.Player).Ships.Count < shipCount))
        {
            return;
        }

        _controller.StartGame();
        _version++;
        StartTurnClock();
        if (IsOnline)
        {
            AddForBothSeats(events, GameEventNames.GameStarted, seat => new GameStartedEvent(BuildView(seat)));
        }
    }

    private void StartTurnClock()
    {
        StopTurnClock();
        if (!IsOnline || _controller.GetState() != GameState.InProgress)
        {
            return;
        }

        var turn = ++_turnId;
        var slow = _seats.First(seat => ReferenceEquals(seat.Player, _controller.GetCurrentPlayer()));
        _turnDeadline = DateTime.UtcNow + _options.TurnTimeout;
        _turnTimer = new Timer(_ => TurnExpired(turn, slow), null, _options.TurnTimeout, Timeout.InfiniteTimeSpan);
    }

    private void StopTurnClock()
    {
        _turnTimer?.Dispose();
        _turnTimer = null;
        _turnDeadline = null;
        _turnId++;
    }

    private void TurnExpired(int turn, Seat slow)
    {
        Execute(events =>
        {
            if (turn != _turnId || _controller.GetState() != GameState.InProgress)
            {
                return;
            }

            _endReason = GameEndReasons.Timeout;
            _logger.LogInformation("Game {Code}: {Name} did not fire in time and loses", Code, slow.Player.Name);
            _controller.EndGame(OtherSeat(slow).Player);
            _version++;
            AddEndEvent(events);
        });
    }

    private void PlayBotTurns()
    {
        while (_controller.GetState() == GameState.InProgress && _seats.Any(seat => seat.IsBot && ReferenceEquals(_controller.GetCurrentPlayer(), seat.Player)))
        {
            _controller.TakeTurn();
        }
    }

    private void AddShotEvents(List<SeatEvent> events, int firstNewShot)
    {
        if (!IsOnline)
        {
            return;
        }

        foreach (var shot in _shots.Skip(firstNewShot))
        {
            AddForBothSeats(events, GameEventNames.ShotResolved, seat => new ShotResolvedEvent(
                ReferenceEquals(shot.Shooter, _seats[seat - 1].Player) ? "you" : "opponent",
                CoordinateDto.From(shot.Target),
                shot.Result,
                BuildView(seat)));
        }

        if (_controller.GetState() == GameState.Finished)
        {
            AddEndEvent(events);
        }
    }

    private void AddEndEvent(List<SeatEvent> events)
    {
        AddForBothSeats(events, GameEventNames.GameEnded, seat => new GameEndedEvent(
            ReferenceEquals(_winner, _seats[seat - 1].Player) ? "you" : "opponent",
            BuildView(seat)));
        StopTurnClock();
        foreach (var seat in _seats)
        {
            seat.StopDropTimer();
        }
    }

    private void AddConnectionEvent(List<SeatEvent> events, Seat changed, bool connected)
    {
        var other = OtherSeat(changed);
        events.Add(new SeatEvent(other.Number, GameEventNames.OpponentConnection, new OpponentConnectionEvent(connected, BuildView(other.Number))));
    }

    private void AddForBothSeats(List<SeatEvent> events, string name, Func<int, object> payload)
    {
        foreach (var seat in _seats)
        {
            events.Add(new SeatEvent(seat.Number, name, payload(seat.Number)));
        }
    }

    private Seat OtherSeat(Seat seat) => _seats[seat.Number == 1 ? 1 : 0];

    private GamePhase Phase => _controller.GetState() switch
    {
        GameState.Setup => _seats[1].IsTaken ? GamePhase.Placing : GamePhase.Lobby,
        GameState.InProgress => GamePhase.InProgress,
        _ => GamePhase.Finished,
    };

    private GameView BuildView(int seat)
    {
        var viewer = _seats[seat - 1];
        return ViewMapper.Build(Code, _version, _controller, viewer, OtherSeat(viewer), Phase, _winner, _endReason, TurnSecondsRemaining());
    }

    private int? TurnSecondsRemaining()
    {
        if (_turnDeadline is not { } deadline)
        {
            return null;
        }

        return Math.Max(0, (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalSeconds));
    }

    private T Execute<T>(Func<List<SeatEvent>, T> command)
    {
        var events = new List<SeatEvent>();
        T result;
        lock (_sync)
        {
            result = command(events);
        }

        if (events.Count > 0)
        {
            _sink.Publish(Code, events);
        }

        return result;
    }

    private void Execute(Action<List<SeatEvent>> command)
    {
        Execute<object?>(events =>
        {
            command(events);
            return null;
        });
    }

    private void EnsurePlacing()
    {
        if (_controller.GetState() != GameState.Setup)
        {
            throw ApiException.Conflict("Ships can only be placed before the game starts.");
        }

        if (Phase == GamePhase.Lobby)
        {
            throw ApiException.Conflict("Wait for the other player to join before placing ships.");
        }
    }

    private void EnsureInProgress()
    {
        switch (_controller.GetState())
        {
            case GameState.Setup:
                throw ApiException.Conflict(Phase == GamePhase.Lobby
                    ? "Wait for the other player to join first."
                    : "Place all your ships before firing.");
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
