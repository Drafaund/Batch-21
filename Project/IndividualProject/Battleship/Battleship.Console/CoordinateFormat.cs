using System.Diagnostics.CodeAnalysis;
using Battleship.Core;

namespace Battleship.ConsoleApp;

/// <summary>Converts between text like <c>D-4</c> (row letter, column number) and <see cref="Coordinate"/>.</summary>
public static class CoordinateFormat
{
    /// <summary>Accepts <c>D-4</c> and <c>D4</c>, any letter case, inside a board of <paramref name="boardSize"/>.</summary>
    public static bool TryParse(string? text, int boardSize, [NotNullWhen(true)] out Coordinate? coordinate)
    {
        coordinate = null;
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed) || !char.IsLetter(trimmed[0]))
        {
            return false;
        }

        var row = char.ToUpperInvariant(trimmed[0]) - 'A';
        var rest = trimmed[1..].TrimStart();
        if (rest.StartsWith('-'))
        {
            rest = rest[1..].TrimStart();
        }

        if (!int.TryParse(rest, out var column) || row >= boardSize || column < 1 || column > boardSize)
        {
            return false;
        }

        coordinate = new Coordinate(column - 1, row);
        return true;
    }

    /// <summary>Formats a coordinate as row letter and column number, such as <c>D-4</c>.</summary>
    public static string Format(Coordinate coordinate)
    {
        return coordinate.Y is >= 0 and < 26
            ? $"{(char)('A' + coordinate.Y)}-{coordinate.X + 1}"
            : $"({coordinate.X},{coordinate.Y})";
    }
}
