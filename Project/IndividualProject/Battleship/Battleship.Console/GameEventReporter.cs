using Battleship.Core;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Battleship.ConsoleApp;

/// <summary>Subscribes to the controller's events to print messages on screen and write them to the log file.</summary>
public class GameEventReporter
{
    private readonly ConsoleScreen _screen;
    private readonly ILogger<GameEventReporter> _logger;

    public GameEventReporter(ConsoleScreen screen, ILogger<GameEventReporter> logger)
    {
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(logger);
        _screen = screen;
        _logger = logger;
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
        _logger.LogInformation("{Shooter} fired at {Target}: {Result}", shooter.Name, cell, result);
    }

    private void HandleGameEnded(IPlayer winner)
    {
        _screen.Write(new Panel(new Markup($"[bold green]{Markup.Escape(winner.Name)}[/] wins the game!")).Border(BoxBorder.Double));
        _logger.LogInformation("{Winner} won the game", winner.Name);
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
