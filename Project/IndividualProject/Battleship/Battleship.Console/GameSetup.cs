namespace Battleship.ConsoleApp;

public record GameSetup(GameMode Mode, IReadOnlyList<string> HumanNames, IReadOnlyList<AiKind> AiKinds);
