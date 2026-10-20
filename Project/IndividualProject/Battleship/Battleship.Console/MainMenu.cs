namespace Battleship.ConsoleApp;

/// <summary>Asks which mode to play and the names of the human players.</summary>
public class MainMenu
{
    private readonly ConsoleScreen _screen;

    public MainMenu(ConsoleScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        _screen = screen;
    }

    public GameSetup Ask()
    {
        _screen.WriteLine("=== BATTLESHIP ===");
        var mode = AskMode();

        var names = mode switch
        {
            GameMode.HumanVsHuman => new[] { AskName("Player 1 name", "Player 1"), AskName("Player 2 name", "Player 2") },
            GameMode.HumanVsAi => new[] { AskName("Your name", "Player") },
            _ => Array.Empty<string>(),
        };

        var aiKinds = mode switch
        {
            GameMode.HumanVsAi => new[] { AskAiKind() },
            GameMode.AiVsAi => new[] { AiKind.Random, AiKind.HuntTarget },
            _ => Array.Empty<AiKind>(),
        };

        return new GameSetup(mode, names, aiKinds);
    }

    private GameMode AskMode()
    {
        while (true)
        {
            _screen.WriteLine("Choose a game mode:");
            _screen.WriteLine("  1. Human vs Human");
            _screen.WriteLine("  2. Human vs AI");
            _screen.WriteLine("  3. AI vs AI");
            _screen.Write("Choice (1-3): ");

            switch (_screen.ReadLine().Trim())
            {
                case "1":
                    return GameMode.HumanVsHuman;
                case "2":
                    return GameMode.HumanVsAi;
                case "3":
                    return GameMode.AiVsAi;
                default:
                    _screen.WriteLine("Please enter 1, 2, or 3.");
                    break;
            }
        }
    }

    private AiKind AskAiKind()
    {
        while (true)
        {
            _screen.WriteLine("Choose your opponent:");
            _screen.WriteLine("  1. Random AI (shoots at random cells)");
            _screen.WriteLine("  2. Hunt-Target AI (hunts at random, then finishes off a ship it hit)");
            _screen.Write("Choice (1-2): ");

            switch (_screen.ReadLine().Trim())
            {
                case "1":
                    return AiKind.Random;
                case "2":
                    return AiKind.HuntTarget;
                default:
                    _screen.WriteLine("Please enter 1 or 2.");
                    break;
            }
        }
    }

    private string AskName(string prompt, string defaultName)
    {
        _screen.Write($"{prompt} (Enter for {defaultName}): ");
        var name = _screen.ReadLine().Trim();
        return name.Length == 0 ? defaultName : name;
    }
}
