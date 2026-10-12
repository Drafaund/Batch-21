namespace Othello.Core;

public interface IGameEvents
{
    event Action GameStarted;
    event Action<Move> MovePlayed;
    event Action<Score> ScoreChanged;
    event Action<DiscColor> TurnChanged;
    event Action<DiscColor> TurnSkipped;
    event Action<GameResult> GameOver;
}
