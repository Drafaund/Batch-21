namespace Battleship.Api;

/// <summary>Settings of the games. Tests shorten the timeout.</summary>
internal sealed class GameOptions
{
    /// <summary>How long a player may be away during a game before the other player wins.</summary>
    public TimeSpan OpponentTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>How long the player whose turn it is may take to fire, in a game between two humans, before losing.</summary>
    public TimeSpan TurnTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>How long a seat must wait between two emotes.</summary>
    public TimeSpan EmoteCooldown { get; set; } = TimeSpan.FromSeconds(2);
}
