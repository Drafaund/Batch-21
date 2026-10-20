namespace Battleship.ConsoleApp;

/// <summary>The only place that talks to <see cref="Console"/>: writing, reading lines, clearing, pausing.</summary>
public class ConsoleScreen
{
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly bool _canClear;
    private readonly TimeSpan _botDelay;

    /// <param name="canClear">False when the output is redirected, where clearing the screen would fail.</param>
    /// <param name="botDelay">Pause after a bot move so a bot game can be watched; zero for no pause.</param>
    public ConsoleScreen(TextReader input, TextWriter output, bool canClear, TimeSpan botDelay)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        _input = input;
        _output = output;
        _canClear = canClear;
        _botDelay = botDelay;
    }

    public void Write(string text) => _output.Write(text);

    public void WriteLine(string text = "") => _output.WriteLine(text);

    /// <exception cref="EndOfStreamException">The input has ended (for example piped input ran out).</exception>
    public string ReadLine() => _input.ReadLine() ?? throw new EndOfStreamException("Input ended.");

    public void Clear()
    {
        if (_canClear)
        {
            Console.Clear();
        }
    }

    public void WaitForEnter(string message)
    {
        Write(message);
        ReadLine();
    }

    public void PauseForBot()
    {
        if (_botDelay > TimeSpan.Zero)
        {
            Thread.Sleep(_botDelay);
        }
    }
}
