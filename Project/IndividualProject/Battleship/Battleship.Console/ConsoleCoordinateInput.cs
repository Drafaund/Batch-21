using Battleship.Core;
using Spectre.Console;

namespace Battleship.ConsoleApp;

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
        var lastLetter = (char)('A' + _boardSize - 1);
        var prompt = new TextPrompt<string>("Target [grey](for example D-4)[/]:")
            .Validate(text => CoordinateFormat.TryParse(text, _boardSize, out _)
                ? ValidationResult.Success()
                : ValidationResult.Error($"[red]Invalid format. Use a letter A-{lastLetter} and a number 1-{_boardSize}, for example D-4.[/]"));

        var answer = _screen.Prompt(prompt);

        // The validator above only lets a parsable answer through.
        CoordinateFormat.TryParse(answer, _boardSize, out var coordinate);
        return coordinate!.Value;
    }
}
