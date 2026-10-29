namespace Battleship.Api;

/// <summary>
/// Names each kind of log entry, so entries can be told apart by name instead of by their wording.
/// </summary>
internal static class LogEvents
{
    public const string EventNameProperty = "EventName";
    public const string GameCodeProperty = "GameCode";

    public const string PlayerJoined = nameof(PlayerJoined);
    public const string ShotFired = nameof(ShotFired);
    public const string GameWon = nameof(GameWon);
    public const string PlayerForfeited = nameof(PlayerForfeited);
    public const string TurnTimedOut = nameof(TurnTimedOut);
    public const string RequestRejected = nameof(RequestRejected);
    public const string EventDeliveryFailed = nameof(EventDeliveryFailed);

    /// <summary>
    /// Returns a logger whose entries carry the given event name.
    /// </summary>
    public static ILogger Event(this ILogger logger, string name)
    {
        return logger.ForContext(EventNameProperty, name);
    }
}
