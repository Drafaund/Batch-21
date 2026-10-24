namespace Battleship.Api;

/// <summary>The names of the SignalR events, as listed in WEB_DESIGN.md.</summary>
internal static class GameEventNames
{
    public const string PlayerJoined = "PlayerJoined";
    public const string GameStarted = "GameStarted";
    public const string ShotResolved = "ShotResolved";
    public const string GameEnded = "GameEnded";
    public const string OpponentConnection = "OpponentConnection";
}
