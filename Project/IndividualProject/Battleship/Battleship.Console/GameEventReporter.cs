using Battleship.Core;
using Serilog;
using Spectre.Console;

namespace Battleship.ConsoleApp;

public class GameEventReporter
{
    private readonly ConsoleScreen _screen;
    private readonly ILogger _logger;

    public GameEventReporter(ConsoleScreen screen, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(logger);
        _screen = screen;
        _logger = logger.ForContext<GameEventReporter>();
    }

    public void Attach(GameController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        controller.OnShotResolved += HandleShotResolved;
        controller.OnGameEnded += HandleGameEnded;
    }

    private void HandleShotResolved(IPlayer shooter, Coordinate target, ShotResult result)
    {
        var cell = CoordinateFormat.Format(target);
        _screen.WriteLine($"[bold]{Markup.Escape(shooter.Name)}[/] fires at [yellow]{cell}[/]: {Describe(result)}");
        _logger.Information("{Shooter} fired at {Target}: {Result}", shooter.Name, cell, result);
    }

    private void HandleGameEnded(IPlayer winner)
    {
        _screen.Write(new Panel(new Markup($"[bold green]{Markup.Escape(winner.Name)}[/] wins the game!")).Border(BoxBorder.Double));
        _logger.Information("{Winner} won the game", winner.Name);
    }

    private static string Describe(ShotResult result)
    {
        return result switch
        {
            ShotResult.Hit => "[bold red]HIT![/]",
            ShotResult.Miss => "[grey]miss.[/]",
            ShotResult.Sunk => "[bold red]HIT and SUNK a ship![/]",
            _ => "[red]invalid shot (outside the board or already fired), try again.[/]",
        };
    }
}
