using Battleship.Core;

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
            _renderer.ShowFleet("YOUR FLEET", _controller.GetBoard(participant.Player));
        }
    }

    private bool AskPlaceRandomly(string name)
    {
        while (true)
        {
            _screen.WriteLine($"{name}, how do you want to place your ships?");
            _screen.WriteLine("  1. Manually");
            _screen.WriteLine("  2. Randomly");
            _screen.Write("Choice (1-2): ");

            switch (_screen.ReadLine().Trim())
            {
                case "1":
                    return false;
                case "2":
                    return true;
                default:
                    _screen.WriteLine("Please enter 1 or 2.");
                    break;
            }
        }
    }

    private void PlaceManually(Participant participant)
    {
        var player = participant.Player;
        var boardSize = _controller.GetBoard(player).BoardSize;
        _screen.WriteLine("Give the first cell of each ship and a direction: H = to the right, V = downward (for example A-1 H).");

        foreach (var ship in participant.Ships)
        {
            _renderer.ShowFleet("YOUR FLEET", _controller.GetBoard(player));
            while (true)
            {
                _screen.Write($"Place your {ship.Type} (length {ship.Length}): ");

                var placement = ParsePlacement(_screen.ReadLine(), boardSize);
                if (placement == null)
                {
                    _screen.WriteLine("Invalid format. Use a cell and H or V, for example A-1 H.");
                }
                else if (_controller.PlaceShip(player, ship, placement.Value.Start, placement.Value.Orientation))
                {
                    break;
                }
                else
                {
                    _screen.WriteLine("The ship does not fit there (outside the board or overlapping another ship).");
                }
            }
        }
    }

    private static (Coordinate Start, Orientation Orientation)? ParsePlacement(string line, int boardSize)
    {
        var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !CoordinateFormat.TryParse(parts[0], boardSize, out var start))
        {
            return null;
        }

        return char.ToUpperInvariant(parts[1][0]) switch
        {
            'H' => (start.Value, Orientation.Horizontal),
            'V' => (start.Value, Orientation.Vertical),
            _ => null,
        };
    }
}
