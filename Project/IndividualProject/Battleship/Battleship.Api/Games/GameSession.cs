using Battleship.Core;

namespace Battleship.Api;

public sealed class GameSession
{
    private readonly object _sync = new();
    private readonly GameController _controller;
    private readonly Seat[] _seats;
    private readonly Random _random;
    private readonly IGameEventSink _sink;
    private readonly GameOptions _options;
    private readonly ILogger _logger;
    private readonly List<ShotRecord> _shots = new();
    private IPlayer? _winner;
    private string? _endReason;
    private Timer? _turnTimer;
    private int _turnId;
    private DateTime? _turnDeadline;
    private int _version = 1;

    internal GameSession(
        string code,
        GameController controller,
        Seat seat1,
        Seat seat2,
        Random random,
        IGameEventSink sink,
        GameOptions options,
        ILogger logger)
    {
        Code = code;
        _controller = controller;
        _seats = new[] { seat1, seat2 };
        _random = random;
        _sink = sink;
        _options = options;
        _logger = logger.ForContext<GameSession>().ForContext(LogEvents.GameCodeProperty, code);

        _controller.OnShotResolved += HandleShotResolved;
        _controller.OnGameEnded += HandleGameEnded;
    }

    public string Code { get; }

    public string PlayerToken => _seats[0].Token!;

    private bool IsOnline => !_seats[1].IsBot;

    public static List<IShip> CreateStandardShips()
    {
        var ships = Enum.GetValues<ShipType>().Select(type => (IShip)new Ship(type)).ToList();

        return ships;
    }

    public int? FindSeat(string? token)
    {
        var seatNumber = _seats.FirstOrDefault(seat => seat.Authorizes(token))?.Number;

        return seatNumber;
    }

    public GameView GetView(int seat)
    {
        lock (_sync)
        {
            return BuildView(seat);
        }
    }

    public Result<JoinGameResponse> Join(string playerName, string token)
    {
        return Execute<Result<JoinGameResponse>>(events =>
        {
            var guest = _seats[1];
            if (!IsOnline || guest.IsTaken)
            {
                return Error.Conflict("This room is already full.");
            }

            guest.Take(playerName, token);
            _version++;
            _logger.Event(LogEvents.PlayerJoined).Information("{PlayerName} joined", playerName);
            events.Add(new SeatEvent(1, GameEventNames.PlayerJoined, new PlayerJoinedEvent(playerName, BuildView(1))));
            return new JoinGameResponse(token, 2, BuildView(2));
        });
    }

    public Result<GameView> PlaceShip(int seat, ShipType type, Coordinate start, Orientation orientation)
    {
        return Execute<Result<GameView>>(events =>
        {
            if (CheckPlacing() is { } notPlacing)
            {
                return notPlacing;
            }

            var player = _seats[seat - 1].Player;
            if (_controller.GetFleet(player).Ships.Any(placed => placed.Type == type))
            {
                return Error.Conflict($"The {type} is already placed.");
            }

            if (!_controller.PlaceShip(player, new Ship(type), start, orientation))
            {
                return Error.Conflict("The ship does not fit there (outside the board or overlapping another ship).");
            }

            StartWhenFleetsAreComplete(events);
            _version++;
            return BuildView(seat);
        });
    }

    public Result<GameView> PlaceShipsRandomly(int seat)
    {
        return Execute<Result<GameView>>(events =>
        {
            if (CheckPlacing() is { } notPlacing)
            {
                return notPlacing;
            }

            var player = _seats[seat - 1].Player;
            if (_controller.GetFleet(player).Ships.Count > 0)
            {
                return Error.Conflict("Random placement is only possible before the first ship is placed.");
            }

            _controller.PlaceShipsRandomly(player, CreateStandardShips(), _random);
            StartWhenFleetsAreComplete(events);
            _version++;
            return BuildView(seat);
        });
    }

    public Result<ShotResponse> Fire(int seat, Coordinate target)
    {
        return Execute<Result<ShotResponse>>(events =>
        {
            if (CheckInProgress() is { } notInProgress)
            {
                return notInProgress;
            }

            var shooter = _seats[seat - 1];
            if (!ReferenceEquals(_controller.GetCurrentPlayer(), shooter.Player))
            {
                return Error.Conflict("It is not your turn.");
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

    public Result SendEmote(int seat, string emote)
    {
        return Execute<Result>(events =>
        {
            var sender = _seats[seat - 1];
            var receiver = OtherSeat(sender);
            if (!IsOnline || !receiver.IsTaken)
            {
                return Error.Conflict(IsOnline
                    ? "Wait for the other player to join before sending emotes."
                    : "Emotes are only for games between two players.");
            }

            var now = DateTime.UtcNow;
            if (sender.LastEmoteAt is { } last && now - last < _options.EmoteCooldown)
            {
                return Error.TooManyRequests("You are sending emotes too fast. Wait a moment.");
            }

            sender.LastEmoteAt = now;
            events.Add(new SeatEvent(receiver.Number, GameEventNames.EmoteReceived, new EmoteReceivedEvent(emote)));
            return Result.Success();
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
                current.DropTimer = StartTimer(_ => Forfeit(current), _options.OpponentTimeout);
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
            _logger.Event(LogEvents.PlayerForfeited).Information("{PlayerName} stayed away and forfeits", absent.Player.Name);
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
        _turnTimer = StartTimer(_ => TurnExpired(turn, slow), _options.TurnTimeout);
    }

    private static Timer StartTimer(TimerCallback callback, TimeSpan dueTime)
    {
        // A timer callback would otherwise run with the request context (scope, trace) of whoever started it.
        using (ExecutionContext.SuppressFlow())
        {
            return new Timer(callback, null, dueTime, Timeout.InfiniteTimeSpan);
        }
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
            _logger.Event(LogEvents.TurnTimedOut).Information("{PlayerName} did not fire in time and loses", slow.Player.Name);
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

    private Error? CheckPlacing()
    {
        if (_controller.GetState() != GameState.Setup)
        {
            return Error.Conflict("Ships can only be placed before the game starts.");
        }

        if (Phase == GamePhase.Lobby)
        {
            return Error.Conflict("Wait for the other player to join before placing ships.");
        }

        return null;
    }

    private Error? CheckInProgress()
    {
        return _controller.GetState() switch
        {
            GameState.Setup => Error.Conflict(Phase == GamePhase.Lobby
                ? "Wait for the other player to join first."
                : "Place all your ships before firing."),
            GameState.Finished => Error.Conflict("The game is over."),
            _ => null,
        };
    }

    private void HandleShotResolved(IPlayer shooter, Coordinate target, ShotResult result)
    {
        _shots.Add(new ShotRecord(shooter, target, result));
        _logger.Event(LogEvents.ShotFired).Information("{PlayerName} fired at {@Target}: {Result}", shooter.Name, target, result);
    }

    private void HandleGameEnded(IPlayer winner)
    {
        _winner = winner;
        _logger.Event(LogEvents.GameWon).Information("{PlayerName} won", winner.Name);
    }
}
