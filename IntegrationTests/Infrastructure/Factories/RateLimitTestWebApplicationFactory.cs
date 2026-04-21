using Microsoft.AspNetCore.Hosting;

namespace IntegrationTests.Infrastructure.Factories;

public class RateLimitWebApplicationFactory : TestWebApplicationFactory
{
    protected override string EnvironmentName => "RateLimitTesting";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        base.ConfigureWebHost(builder);
    }
}