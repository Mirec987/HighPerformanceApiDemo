using IntegrationTests.Infrastructure.Factories;

public abstract class IntegrationTestBase<TFactory> : IClassFixture<TFactory>, IAsyncLifetime
    where TFactory : TestWebApplicationFactory
{
    protected readonly TFactory Factory;
    protected HttpClient Client { get; private set; } = default!;

    protected IntegrationTestBase(TFactory factory)
    {
        Factory = factory;
    }

    public async Task InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
        Client = Factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}