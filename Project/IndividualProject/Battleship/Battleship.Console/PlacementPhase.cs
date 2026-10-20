using Battleship.Core;
using Spectre.Console;

namespace Battleship.ConsoleApp;

/// <summary>Places one participant's ships: bots always at random, humans manually or at random.</summary>
public class PlacementPhase
{
    private readonly ConsoleScreen _screen;
    private readonly BoardRenderer _renderer;
    private readonly GameController _controller;
    private readonly Random _random;

    public PlacementPhase(ConsoleScreen screen, BoardRenderer renderer, GameController controller, Random random)
    {
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(random);
        _screen = screen;
        _renderer = renderer;
        _controller = controller;
        _random = random;
    }

    public void Place(Participant participant)
    {
        if (!participant.IsHuman || AskPlaceRandomly(participant.Player.Name))
        {
            _controller.PlaceShipsRandomly(participant.Player, participant.Ships, _random);
        }
        else
        {
            PlaceManually(participant);
        }

        if (participant.IsHuman)
        {
            _renderer.ShowFleet("Your fleet", _controller.GetBoard(participant.Player));
        }
    }

    private bool AskPlaceRandomly(string name)
    {
        return _screen.Prompt(new SelectionPrompt<bool>()
            .Title($"[bold]{Markup.Escape(name)}[/], how do you want to place your [green]ships[/]?")
            .AddChoices(false, true)
            .UseConverter(randomly => randomly ? "Randomly" : "Manually"));
    }

    private void PlaceManually(Participant participant)
    {
        var player = participant.Player;
        var boardSize = _controller.GetBoard(player).BoardSize;

        foreach (var ship in participant.Ships)
        {
            _renderer.ShowFleet("Your fleet", _controller.GetBoard(player));
            while (true)
            {
                var start = AskStartCell(ship, boardSize);
                var orientation = AskOrientation();
                if (_controller.PlaceShip(player, ship, start, orientation))
                {
                    break;
                }

                _screen.WriteLine("[red]The ship does not fit there (outside the board or overlapping another ship). Try again.[/]");
            }
        }
    }

    private Coordinate AskStartCell(IShip ship, int boardSize)
    {
        var lastLetter = (char)('A' + boardSize - 1);
        var prompt = new TextPrompt<string>($"First cell of your [green]{ship.Type}[/] (length {ship.Length}) [grey](for example A-1)[/]:")
            .Validate(text => CoordinateFormat.TryParse(text, boardSize, out _)
                ? ValidationResult.Success()
                : ValidationResult.Error($"[red]Invalid format. Use a letter A-{lastLetter} and a number 1-{boardSize}, for example A-1.[/]"));

        // The validator above only lets a parsable answer through.
        CoordinateFormat.TryParse(_screen.Prompt(prompt), boardSize, out var start);
        return start!.Value;
    }

    private Orientation AskOrientation()
    {
        return _screen.Prompt(new SelectionPrompt<Orientation>()
            .Title("Direction:")
            .AddChoices(Orientation.Horizontal, Orientation.Vertical)
            .UseConverter(orientation => orientation == Orientation.Horizontal ? "Horizontal (to the right)" : "Vertical (downward)"));
    }
}
