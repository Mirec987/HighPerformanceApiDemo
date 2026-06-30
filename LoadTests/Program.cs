using System.Net;
using System.Net.Http.Json;
using LoadTests;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

var baseUrl = GetBaseUrl(args);
var profile = (Environment.GetEnvironmentVariable("LOAD_PROFILE") ?? "baseline").ToLowerInvariant();
using var httpClient = new HttpClient { BaseAddress = baseUrl, Timeout = TimeSpan.FromSeconds(30) };

Console.WriteLine($"Running '{profile}' load profile against {baseUrl}");
await EnsureApiIsAvailableAsync(httpClient);
var seededOrderId = await CreateSeedOrderAsync(httpClient);

var ordersRead = Scenario.Create("orders_read", async context =>
    ToResponse(await httpClient.GetAsync("/api/orders?page=1&pageSize=20", context.ScenarioCancellationToken)));

var orderDetailRead = Scenario.Create("order_detail_read", async context =>
    ToResponse(await httpClient.GetAsync($"/api/orders/{seededOrderId}", context.ScenarioCancellationToken)));

var customersRead = Scenario.Create("customers_read", async context =>
    ToResponse(await httpClient.GetAsync("/api/customers?take=20", context.ScenarioCancellationToken)));

var ordersWrite = Scenario.Create("orders_write", async context =>
{
    using var response = await httpClient.PostAsJsonAsync(
        "/api/orders", CreateOrderRequest.Create(), context.ScenarioCancellationToken);

    return response.StatusCode == HttpStatusCode.Created
        ? Response.Ok(statusCode: response.StatusCode.ToString())
        : Response.Fail(statusCode: response.StatusCode.ToString());
});

if (profile == "stress")
{
    ordersRead = ConfigureStress(ordersRead, 100);
    orderDetailRead = ConfigureStress(orderDetailRead, 50);
    customersRead = ConfigureStress(customersRead, 25);
    ordersWrite = ConfigureStress(ordersWrite, 10);
}
else if (profile == "spike")
{
    ordersRead = ConfigureSpike(ordersRead, 20, 150);
    orderDetailRead = ConfigureSpike(orderDetailRead, 10, 75);
    customersRead = ConfigureSpike(customersRead, 5, 40);
    ordersWrite = ConfigureSpike(ordersWrite, 1, 10);
}
else
{
    profile = "baseline";
    ordersRead = ConfigureBaseline(ordersRead, 20);
    orderDetailRead = ConfigureBaseline(orderDetailRead, 10);
    customersRead = ConfigureBaseline(customersRead, 5);
    ordersWrite = ConfigureBaseline(ordersWrite, 1);
}

var reportName = $"order-management-{profile}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
var stats = NBomberRunner
    .RegisterScenarios(ordersRead, orderDetailRead, customersRead, ordersWrite)
    .WithTestSuite("order-management-api")
    .WithTestName(profile)
    .WithReportFolder("load-test-results")
    .WithReportFileName(reportName)
    .WithReportFormats(ReportFormat.Txt, ReportFormat.Html, ReportFormat.Csv)
    .Run();

if (profile == "baseline")
{
    var failures = new List<string>();
    ValidateScenario(stats.ScenarioStats.Get("orders_read"), PerformanceSla.MinReadRequestsPerSecond, failures);
    ValidateScenario(stats.ScenarioStats.Get("order_detail_read"), 10, failures);
    ValidateScenario(stats.ScenarioStats.Get("customers_read"), 5, failures);
    ValidateScenario(stats.ScenarioStats.Get("orders_write"), PerformanceSla.MinWriteRequestsPerSecond, failures);

    if (failures.Count > 0)
    {
        Console.Error.WriteLine("SLA FAILED:");
        failures.ForEach(failure => Console.Error.WriteLine($"- {failure}"));
        Environment.ExitCode = 1;
    }
    else
    {
        Console.WriteLine("All baseline SLA thresholds passed.");
    }
}

static ScenarioProps ConfigureBaseline(ScenarioProps scenario, int rate) =>
    scenario.WithWarmUpDuration(TimeSpan.FromSeconds(5)).WithLoadSimulations(
        Simulation.Inject(rate, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)));

static ScenarioProps ConfigureStress(ScenarioProps scenario, int targetRate) =>
    scenario.WithWarmUpDuration(TimeSpan.FromSeconds(10)).WithLoadSimulations(
        Simulation.RampingInject(targetRate, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2)));

static ScenarioProps ConfigureSpike(ScenarioProps scenario, int normalRate, int spikeRate) =>
    scenario.WithWarmUpDuration(TimeSpan.FromSeconds(5)).WithLoadSimulations(
        Simulation.Inject(normalRate, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20)),
        Simulation.Inject(spikeRate, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)),
        Simulation.Inject(normalRate, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20)));

static IResponse ToResponse(HttpResponseMessage response)
{
    using (response)
    {
        return response.IsSuccessStatusCode
            ? Response.Ok(statusCode: response.StatusCode.ToString())
            : Response.Fail(statusCode: response.StatusCode.ToString());
    }
}

static void ValidateScenario(ScenarioStats stats, double minRps, ICollection<string> failures)
{
    if (stats.Ok.Latency.Percent95 > PerformanceSla.MaxP95Milliseconds)
        failures.Add($"{stats.ScenarioName}: p95 {stats.Ok.Latency.Percent95} ms > {PerformanceSla.MaxP95Milliseconds} ms");
    if (stats.Ok.Latency.Percent99 > PerformanceSla.MaxP99Milliseconds)
        failures.Add($"{stats.ScenarioName}: p99 {stats.Ok.Latency.Percent99} ms > {PerformanceSla.MaxP99Milliseconds} ms");
    if (stats.Fail.Request.Percent >= PerformanceSla.MaxErrorPercentage)
        failures.Add($"{stats.ScenarioName}: errors {stats.Fail.Request.Percent}% >= {PerformanceSla.MaxErrorPercentage}%");
    if (stats.Ok.Request.RPS < minRps)
        failures.Add($"{stats.ScenarioName}: throughput {stats.Ok.Request.RPS} req/s < {minRps} req/s");
}

static Uri GetBaseUrl(string[] args)
{
    var url = args.FirstOrDefault(x => Uri.TryCreate(x, UriKind.Absolute, out _))
        ?? Environment.GetEnvironmentVariable("BASE_URL")
        ?? "https://localhost:7088";
    return new Uri(url.TrimEnd('/'));
}

static async Task EnsureApiIsAvailableAsync(HttpClient client)
{
    try
    {
        using var response = await client.GetAsync("/health/live");
        response.EnsureSuccessStatusCode();
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"API is not reachable at {client.BaseAddress}.", ex);
    }
}

static async Task<Guid> CreateSeedOrderAsync(HttpClient client)
{
    using var response = await client.PostAsJsonAsync("/api/orders", CreateOrderRequest.Create());
    response.EnsureSuccessStatusCode();
    var order = await response.Content.ReadFromJsonAsync<CreatedOrderResponse>();
    return order?.Id ?? throw new InvalidOperationException("Seed order response did not contain an id.");
}

internal sealed record CreatedOrderResponse(Guid Id);
internal sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyCollection<CreateOrderItemRequest> Items)
{
    public static CreateOrderRequest Create() => new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        [new CreateOrderItemRequest(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 1)]);
}
internal sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);
