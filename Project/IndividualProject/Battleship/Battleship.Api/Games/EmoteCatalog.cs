namespace Battleship.Api;

internal static class EmoteCatalog
{
    public static IReadOnlyList<string> Codes { get; } = new[]
    {
        "thumbsUp",
        "laugh",
        "wow",
        "angry",
        "target",
        "wave",
        "handshake",
        "clap",
    };

    public static bool IsKnown(string? code) => code != null && Codes.Contains(code);
}
