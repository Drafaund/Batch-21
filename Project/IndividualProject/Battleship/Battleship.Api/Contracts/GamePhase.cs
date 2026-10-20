namespace Battleship.Api;

/// <summary>Where a game is, as the API describes it. Lobby is used once games between two humans exist.</summary>
internal enum GamePhase
{
    Lobby,
    Placing,
    InProgress,
    Finished,
}
