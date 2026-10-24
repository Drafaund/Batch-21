namespace Battleship.Api;

/// <summary>
/// The emotes a player may send. The server accepts only these codes, so nothing a player types ever reaches the
/// opponent. The front end turns a code into a picture.
/// </summary>
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
