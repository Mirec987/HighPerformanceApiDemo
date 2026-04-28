# Order Management API (High Performance Demo)

ASP.NET Core + EF Core backend focused on performance and clean architecture.

---

## Features
- Create/list/read orders + status update
- FluentValidation, ProblemDetails  
- Serilog + CorrelationId  
- Rate limiting, Health checks  
- Optimistic concurrency  
- Integration tests for success, validation, not found, rate limiting, and concurrency

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

Swagger:  
https://localhost:xxxx/swagger

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

---

## Testing
- WebApplicationFactory  
- SQLite in-memory  
- `dotnet test OrderManagement.sln --no-restore`

---

## Performance testing
- NBomber load tests are in `LoadTests`
- Start the API first. Use `LoadTesting` to raise the write rate limit for POST benchmarks:
```bash
$env:ASPNETCORE_ENVIRONMENT="LoadTesting"
dotnet run --project OrderManagement.Api --urls "http://localhost:5088"
```
- Run the load tests against the API URL:
```bash
dotnet run --project LoadTests -- http://localhost:5088
```
- Or use an environment variable:
```bash
$env:BASE_URL="http://localhost:5088"
dotnet run --project LoadTests
```
- Reports are written to `LoadTests/load-test-results`

---

## Purpose
Modern .NET backend demo.
