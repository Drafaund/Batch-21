using Spectre.Console;

namespace Battleship.ConsoleApp;

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
        _screen.Write(new FigletText("Battleship").Color(Color.Blue));
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
        return _screen.Prompt(new SelectionPrompt<GameMode>()
            .Title("Choose a [green]game mode[/]:")
            .AddChoices(GameMode.HumanVsHuman, GameMode.HumanVsAi, GameMode.AiVsAi)
            .UseConverter(mode => mode switch
            {
                GameMode.HumanVsHuman => "Human vs Human",
                GameMode.HumanVsAi => "Human vs AI",
                _ => "AI vs AI",
            }));
    }

    private AiKind AskAiKind()
    {
        return _screen.Prompt(new SelectionPrompt<AiKind>()
            .Title("Choose your [green]opponent[/]:")
            .AddChoices(AiKind.Random, AiKind.HuntTarget)
            .UseConverter(kind => kind == AiKind.Random
                ? "Random AI [grey](shoots at random cells)[/]"
                : "Hunt-Target AI [grey](hunts at random, then finishes off a ship it hit)[/]"));
    }

    private string AskName(string prompt, string defaultName)
    {
        var name = _screen.Prompt(new TextPrompt<string>(prompt + ":").DefaultValue(defaultName)).Trim();
        return name.Length == 0 ? defaultName : name;
    }
}
