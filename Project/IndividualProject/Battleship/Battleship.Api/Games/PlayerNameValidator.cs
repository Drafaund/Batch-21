namespace Battleship.Api;

internal static class PlayerNameValidator
{
    public const int MaxLength = 20;

    public static string Validate(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > MaxLength || trimmed.Any(char.IsControl))
        {
            throw ApiException.BadRequest($"playerName must be 1 to {MaxLength} characters without control characters.");
        }

        return trimmed;
    }
}
