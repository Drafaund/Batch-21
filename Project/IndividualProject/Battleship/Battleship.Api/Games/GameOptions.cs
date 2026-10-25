namespace Battleship.Api;

internal sealed class GameOptions
{
    public TimeSpan OpponentTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan TurnTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan EmoteCooldown { get; set; } = TimeSpan.FromSeconds(2);
}
