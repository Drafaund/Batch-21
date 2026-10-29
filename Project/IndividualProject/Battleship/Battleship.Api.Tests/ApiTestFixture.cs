namespace Battleship.Api.Tests;

/// <summary>
/// Gives a test class one <typeparamref name="TFactory"/> (so one <see cref="LogCapture"/> and one game store) for all of
/// its tests, and a fresh <see cref="HttpClient"/> for each test.
/// </summary>
public abstract class ApiTestFixture<TFactory>
    where TFactory : ApiFactory, new()
{
    protected TFactory Factory { get; private set; } = null!;

    protected HttpClient Http { get; private set; } = null!;

    [OneTimeSetUp]
    public void CreateFactory()
    {
        Factory = new TFactory();
    }

    [OneTimeTearDown]
    public async Task DisposeFactory()
    {
        await Factory.DisposeAsync();
    }

    [SetUp]
    public void CreateClient()
    {
        Http = Factory.CreateClient();
    }
}
