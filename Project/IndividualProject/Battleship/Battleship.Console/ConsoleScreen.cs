using Spectre.Console;
using Spectre.Console.Rendering;

namespace Battleship.ConsoleApp;

/// <summary>The only place that talks to the terminal (through Spectre.Console): writing, prompting, clearing, pausing.</summary>
public class ConsoleScreen
{
    private readonly IAnsiConsole _console;
    private readonly TimeSpan _botDelay;

    /// <param name="console">The Spectre console to write to and read from.</param>
    /// <param name="botDelay">Pause after a bot move so a bot game can be watched; zero for no pause.</param>
    public ConsoleScreen(IAnsiConsole console, TimeSpan botDelay)
    {
        ArgumentNullException.ThrowIfNull(console);
        _console = console;
        _botDelay = botDelay;
    }

    /// <summary>The width of the terminal in columns.</summary>
    public int Width => _console.Profile.Width;

    /// <summary>Writes a line of Spectre markup. Escape any user text first with <see cref="Markup.Escape"/>.</summary>
    public void WriteLine(string markup = "") => _console.MarkupLine(markup);

    /// <summary>Writes a Spectre renderable such as a table or a panel.</summary>
    public void Write(IRenderable renderable) => _console.Write(renderable);

    /// <summary>Shows a Spectre prompt and returns the answer.</summary>
    public T Prompt<T>(IPrompt<T> prompt) => _console.Prompt(prompt);

    /// <summary>Clears the terminal when the output is a real terminal.</summary>
    public void Clear()
    {
        if (_console.Profile.Out.IsTerminal)
        {
            _console.Clear();
        }
    }

    /// <summary>Shows <paramref name="markup"/> and waits for Enter.</summary>
    public void WaitForEnter(string markup)
    {
        _console.Prompt(new TextPrompt<string>(markup).AllowEmpty());
    }

    /// <summary>Waits a moment so a bot move can be seen.</summary>
    public void PauseForBot()
    {
        if (_botDelay > TimeSpan.Zero)
        {
            Thread.Sleep(_botDelay);
        }
    }
}
