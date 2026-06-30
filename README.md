# Order Management API (High Performance Demo)

ASP.NET Core + EF Core backend focused on performance and clean architecture.

---

## Features

- Create/list/read orders + status update
- FluentValidation, ProblemDetails
- Serilog + CorrelationId
- Rate limiting, Health checks
- Optimistic concurrency
- IMemoryCache for order detail and filtered lists
- Integration tests for success, validation, caching, rate limiting, and concurrency

---

## Architecture

```text
Api -> Application -> Domain
Api -> Infrastructure -> Domain
Infrastructure -> Application
```

---

## Run

```bash
dotnet ef database update
dotnet run
```

Swagger: `https://localhost:xxxx/swagger`

---

## Endpoints

```http
POST   /api/orders
GET    /api/orders
GET    /api/orders/{id}
PATCH  /api/orders/{id}/status
```

---

## Concepts

- Projection, AsNoTracking
- Paging / filtering
- Request validation, logging, error handling
- Rate limiting (429), concurrency (409)
- DTO caching, short expiration, versioned list keys

---

## Testing

- WebApplicationFactory
- SQLite in-memory
- Cache invalidation integration tests
- `dotnet test OrderManagement.sln --no-restore`

---

## Caching

Frequently read order data is cached through an application-level `ICacheService` implemented with `IMemoryCache`:

- order detail DTOs expire after 60 seconds
- filtered order lists expire after 20 seconds
- updating an order removes its detail cache entry
- creating or updating an order increments the list cache version
- concurrent misses for the same key share one database operation
- missing orders are not cached

Configuration is stored in `OrderManagement.Api/appsettings.json`:

```json
"Caching": {
  "Enabled": true,
  "OrderDetailSeconds": 60,
  "OrderListSeconds": 20
}
```

Caching can be disabled for comparison without changing code:

```powershell
$env:Caching__Enabled="false"
```

### Local NBomber comparison

The same 30-second baseline was run locally with caching disabled and enabled:

| Scenario | Metric | Without cache | With cache | Change |
|---|---:|---:|---:|---:|
| Order list | mean | 2.40 ms | 1.07 ms | -55% |
| Order list | p95 | 3.78 ms | 2.73 ms | -28% |
| Order detail | mean | 2.06 ms | 0.98 ms | -52% |
| Order detail | p95 | 3.56 ms | 1.60 ms | -55% |

These are local measurements, not production guarantees. Longer read-only runs on consistent hardware should be used for stable p99 comparisons.

---

## Performance testing

The repository includes:

- NBomber HTTP load tests in `LoadTests`
- `baseline`, `stress`, and `spike` load profiles
- automatic p95, p99, throughput, and error-rate SLA checks
- BenchmarkDotNet microbenchmarks in `Benchmarks`
- `dotnet-counters` and `dotnet-trace` scripts in `scripts`

Start the API in `LoadTesting` mode to raise the write rate limit:

```powershell
$env:ASPNETCORE_ENVIRONMENT="LoadTesting"
dotnet run --project OrderManagement.Api -c Release --urls "http://localhost:5088"
```

Run the baseline load profile:

```powershell
$env:BASE_URL="http://localhost:5088"
$env:LOAD_PROFILE="baseline"
dotnet run --project LoadTests -c Release
```

Run all microbenchmarks:

```powershell
dotnet run --project Benchmarks -c Release -- --filter "*"
```

Generated reports and profiling captures are ignored by Git. See [PERFORMANCE.md](PERFORMANCE.md) for SLA values, all profiles, report locations, and profiling instructions.

---

## Purpose

Modern .NET backend demo.
