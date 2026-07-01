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

## Run locally

### Prerequisites

- .NET 9 SDK
- SQL Server Express LocalDB (the default connection string uses `(localdb)\MSSQLLocalDB`)
- Git
- EF Core CLI: `dotnet tool install --global dotnet-ef`

### Setup

```powershell
git clone <repository-url>
cd OrderManagement
dotnet restore
dotnet ef database update --project OrderManagement.Infrastructure --startup-project OrderManagement.Api
dotnet run --project OrderManagement.Api --launch-profile https
```

Swagger is available at `https://localhost:7282/swagger` (or `http://localhost:5043/swagger`).

If the local HTTPS certificate is not trusted, run:

```powershell
dotnet dev-certs https --trust
```

Run the complete test suite:

```powershell
dotnet test OrderManagement.sln --no-restore
```

The default database connection is configured in `OrderManagement.Api/appsettings.json`. Override `ConnectionStrings__DefaultConnection` when LocalDB is unavailable.

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
- Cache synchronization, size-limit, and request immutability tests
- Current result: 14/14 tests passing
- `dotnet test OrderManagement.sln --no-restore`

---

## Caching

Frequently read order data is cached through an application-level `ICacheService` implemented with `IMemoryCache`:

- order detail DTOs expire after 60 seconds
- filtered order lists expire after 20 seconds
- updating an order removes its detail cache entry
- creating or updating an order increments the list cache version
- per-key semaphores serialize concurrent misses without sharing a request cancellation token
- cache removal also runs when caching is disabled, preventing stale entries after configuration changes
- each entry has `Size = 1` and the cache has a configurable global size limit
- missing orders are not cached

Configuration is stored in `OrderManagement.Api/appsettings.json`:

```json
"Caching": {
  "Enabled": true,
  "OrderDetailSeconds": 60,
  "OrderListSeconds": 20,
  "SizeLimit": 10000
}
```

`SizeLimit` is expressed in logical units. Because every current entry uses `Size = 1`, the default allows up to 10,000 entries before memory-cache compaction.

List paging is normalized on a copied `GetOrdersRequest`: invalid page values use page 1, page size defaults to 20, and the maximum page size is 100. The caller's request object is never modified.

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

## Demo scope and roadmap

This repository is a performance-focused demo and learning project, not a production-ready deployment template. It demonstrates the current API architecture, database access, caching, validation, rate limiting, health checks, logging, load testing, benchmarking, and profiling.

Production-oriented capabilities are intentionally incomplete and may be added in future iterations:

- OpenTelemetry tracing and metrics with a Prometheus-compatible backend
- Gzip/Brotli response compression and measurements of its impact
- JWT/OAuth2 authentication and authorization, HTTPS enforcement, and stricter CORS policies
- distributed caching such as Redis for horizontally scaled instances
- reverse proxy and load-balancing configuration such as Nginx or YARP
- production secret management, deployment configuration, and CI/CD automation
- additional resilience, security, observability, and capacity testing

Performance results in this repository are local measurements and should not be treated as production guarantees.
