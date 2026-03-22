namespace OrderManagement.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactory>
{
    protected readonly HttpClient Client;

    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Client = factory.CreateClient();
    }
}