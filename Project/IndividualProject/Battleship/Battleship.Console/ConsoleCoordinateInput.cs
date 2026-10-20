using Battleship.Core;
using Spectre.Console;

namespace Battleship.ConsoleApp;

/// <summary>Reads a target such as <c>D-4</c> or <c>D4</c> with a Spectre.Console prompt that asks again until the format is right.</summary>
public class ConsoleCoordinateInput : ICoordinateInput
{
    private readonly ConsoleScreen _screen;
    private readonly int _boardSize;

    /// <summary>Creates an input that prompts on <paramref name="screen"/> for a board of <paramref name="boardSize"/> cells per side.</summary>
    public ConsoleCoordinateInput(ConsoleScreen screen, int boardSize)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
        _boardSize = boardSize;
    }

    /// <inheritdoc />
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
