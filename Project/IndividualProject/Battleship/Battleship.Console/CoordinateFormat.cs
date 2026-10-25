using System.Diagnostics.CodeAnalysis;
using Battleship.Core;

namespace Battleship.ConsoleApp;

public static class CoordinateFormat
{
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

    public static string Format(Coordinate coordinate)
    {
        return coordinate.Y is >= 0 and < 26
            ? $"{(char)('A' + coordinate.Y)}-{coordinate.X + 1}"
            : $"({coordinate.X},{coordinate.Y})";
    }
}
