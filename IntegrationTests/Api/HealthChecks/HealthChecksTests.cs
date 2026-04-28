using System.Net;
using FluentAssertions;
using IntegrationTests.Infrastructure.Factories;

namespace IntegrationTests.Api.HealthChecks;

public class HealthChecksTests : IntegrationTestBase<TestWebApplicationFactory>
{
    public HealthChecksTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Live_HealthCheck_Should_Return_Ok()
    {
        var response = await Client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}