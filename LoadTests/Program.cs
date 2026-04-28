using System.Net;
using System.Net.Http.Json;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

var baseUrl = GetBaseUrl(args);
using var httpClient = new HttpClient
{
    BaseAddress = baseUrl,
    Timeout = TimeSpan.FromSeconds(30)
};

Console.WriteLine($"Running load tests against {baseUrl}");
await EnsureApiIsAvailableAsync(httpClient);

var readScenario = Scenario.Create("orders_read_baseline", async context =>
{
    try
    {
        var response = await httpClient.GetAsync("/api/orders?page=1&pageSize=20", context.ScenarioCancellationToken);

        return response.IsSuccessStatusCode
            ? Response.Ok(statusCode: response.StatusCode.ToString())
            : Response.Fail(statusCode: response.StatusCode.ToString());
    }
    catch (TaskCanceledException)
    {
        return Response.Fail(statusCode: "timeout");
    }
})
.WithWarmUpDuration(TimeSpan.FromSeconds(5))
.WithLoadSimulations(
    Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)));

var writeScenario = Scenario.Create("orders_write_smoke", async context =>
{
    var request = new CreateOrderRequest(
        CustomerId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Items:
        [
            new CreateOrderItemRequest(
                ProductId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Quantity: 1)
        ]);

    try
    {
        var response = await httpClient.PostAsJsonAsync("/api/orders", request, context.ScenarioCancellationToken);

        return response.StatusCode is HttpStatusCode.Created or HttpStatusCode.TooManyRequests
            ? Response.Ok(statusCode: response.StatusCode.ToString())
            : Response.Fail(statusCode: response.StatusCode.ToString());
    }
    catch (TaskCanceledException)
    {
        return Response.Fail(statusCode: "timeout");
    }
})
.WithWarmUpDuration(TimeSpan.FromSeconds(5))
.WithLoadSimulations(
    Simulation.Inject(rate: 1, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(15)));

NBomberRunner
    .RegisterScenarios(readScenario, writeScenario)
    .WithReportFolder("load-test-results")
    .WithReportFileName($"order-management-{DateTime.UtcNow:yyyyMMdd-HHmmss}")
    .WithReportFormats(ReportFormat.Txt, ReportFormat.Html)
    .Run();

static Uri GetBaseUrl(string[] args)
{
    var configuredUrl =
        args.FirstOrDefault(x => Uri.TryCreate(x, UriKind.Absolute, out _)) ??
        Environment.GetEnvironmentVariable("BASE_URL") ??
        "https://localhost:7088";

    return new Uri(configuredUrl.TrimEnd('/'));
}

static async Task EnsureApiIsAvailableAsync(HttpClient httpClient)
{
    try
    {
        using var response = await httpClient.GetAsync("/health/live");

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Health check failed with {(int)response.StatusCode} {response.StatusCode}.");
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            $"API is not reachable at {httpClient.BaseAddress}. Start OrderManagement.Api before running load tests.",
            ex);
    }
}

internal sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyCollection<CreateOrderItemRequest> Items);

internal sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);