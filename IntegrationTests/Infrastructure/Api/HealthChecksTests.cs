using FluentAssertions;
using OrderManagement.IntegrationTests.Infrastructure;
using System.Net;

namespace OrderManagement.IntegrationTests.Api;

public class HealthChecksTests : IntegrationTestBase
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