namespace Battleship.ConsoleApp;

/// <summary>The menu's answer: the mode, a name for each human player, and the kind of each bot, in seat order.</summary>
public record GameSetup(GameMode Mode, IReadOnlyList<string> HumanNames, IReadOnlyList<AiKind> AiKinds);
