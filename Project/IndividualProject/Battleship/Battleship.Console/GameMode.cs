namespace Battleship.ConsoleApp;

/// <summary>How many of the two players are human.</summary>
public enum GameMode
{
    /// <summary>Two humans share one terminal (hot-seat).</summary>
    HumanVsHuman,
    /// <summary>One human plays against a bot.</summary>
    HumanVsAi,
    /// <summary>Two bots play and the human watches.</summary>
    AiVsAi,
}
