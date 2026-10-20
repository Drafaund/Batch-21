using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Battleship.Api.Tests;

/// <summary>
/// Starts the API in memory for a test class. Every game gets its own <see cref="Random"/> with a
/// seed that counts up, so the bot's ship layout and shots are the same on every run.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private int _seed;

    public ApiFactory()
        : this(1000)
    {
    }

    protected ApiFactory(int firstSeed)
    {
        _seed = firstSeed;
    }

    /// <summary>Captures every log message the API writes.</summary>
    public LogCapture Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<Func<Random>>();
            services.AddSingleton<Func<Random>>(_ => () => new Random(Interlocked.Increment(ref _seed)));
            services.AddSingleton<ILoggerProvider>(Logs);
        });
    }
}
