using Battleship.ConsoleApp;
using Battleship.Core;
using Microsoft.Extensions.Logging;
using Serilog;

// Composition root: every concrete object of the game is created here, including the five ships of each player.

const int BotPauseMilliseconds = 700;

var screen = new ConsoleScreen(
    Console.In,
    Console.Out,
    canClear: !Console.IsOutputRedirected,
    botDelay: Console.IsInputRedirected ? TimeSpan.Zero : TimeSpan.FromMilliseconds(BotPauseMilliseconds));

using var serilog = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs", "battleship-.log"), rollingInterval: RollingInterval.Day)
    .CreateLogger();
using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(serilog));

try
{
    var setup = new MainMenu(screen).Ask();

    var random = new Random();
    var input = new ConsoleCoordinateInput(screen, new Board().BoardSize);

    var (first, second) = setup.Mode switch
    {
        GameMode.HumanVsHuman => (CreateHuman(1, setup.HumanNames[0]), CreateHuman(2, setup.HumanNames[1])),
        GameMode.HumanVsAi => (CreateHuman(1, setup.HumanNames[0]), CreateBot(2, setup.AiKinds[0])),
        _ => (CreateBot(1, setup.AiKinds[0]), CreateBot(2, setup.AiKinds[1])),
    };

    var controller = new GameController(first.Player, first.Strategy, second.Player, second.Strategy);
    new GameEventReporter(screen, loggerFactory.CreateLogger<GameEventReporter>()).Attach(controller);

    var renderer = new BoardRenderer(screen);
    var placement = new PlacementPhase(screen, renderer, controller, random);
    new GameRunner(controller, first, second, screen, renderer, placement).Run();

    Participant CreateHuman(int id, string name)
    {
        var player = new HumanPlayer(id, name, new Board(), new Fleet(), input);
        return new Participant(player, player, IsHuman: true, CreateStandardShips());
    }

    Participant CreateBot(int id, AiKind kind)
    {
        Player player = kind == AiKind.Random
            ? new RandomAIPlayer(id, "Bot Random", new Board(), new Fleet(), random)
            : new HuntTargetAIPlayer(id, "Bot Hunter", new Board(), new Fleet(), random);
        return new Participant(player, (IShotStrategy)player, IsHuman: false, CreateStandardShips());
    }

    IReadOnlyList<IShip> CreateStandardShips()
    {
        return Enum.GetValues<ShipType>().Select(type => (IShip)new Ship(type)).ToList();
    }
}
catch (EndOfStreamException)
{
    screen.WriteLine();
    screen.WriteLine("Input ended. Exiting game.");
}
