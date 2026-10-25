using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Battleship.Api.Tests;

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
