using Battleship.Core;

namespace Battleship.ConsoleApp;

/// <summary>Reads a target such as <c>D-4</c> or <c>D4</c> from the console, asking again until the format is right.</summary>
public class ConsoleCoordinateInput : ICoordinateInput
{
    private readonly ConsoleScreen _screen;
    private readonly int _boardSize;

    public ConsoleCoordinateInput(ConsoleScreen screen, int boardSize)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        _boardSize = boardSize;
    }

    public Coordinate ReadCoordinate()
    {
        while (true)
        {
            _screen.Write("Target (for example D-4): ");
            if (CoordinateFormat.TryParse(_screen.ReadLine(), _boardSize, out var coordinate))
            {
                return coordinate.Value;
            }

            var lastLetter = (char)('A' + _boardSize - 1);
            _screen.WriteLine($"Invalid format. Use a letter A-{lastLetter} and a number 1-{_boardSize}, for example D-4.");
        }
    }
}
