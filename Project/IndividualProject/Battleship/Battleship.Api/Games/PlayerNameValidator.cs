namespace Battleship.Api;

/// <summary>Checks the name a player types.</summary>
internal static class PlayerNameValidator
{
    public const int MaxLength = 20;

    /// <summary>Returns the trimmed name, or throws a 400 when it is empty, too long, or has control characters.</summary>
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
