namespace Battleship.Api;

internal static class PlayerNameValidator
{
    public const int MaxLength = 20;

    public static Result<string> Validate(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > MaxLength || trimmed.Any(char.IsControl))
        {
            return Error.Validation($"playerName must be 1 to {MaxLength} characters without control characters.");
        }

        return trimmed;
    }
}
