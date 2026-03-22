# Order Management API (High Performance Demo)

ASP.NET Core + EF Core backend focused on performance and clean architecture.

---

## Features
- CRUD orders + status update  
- FluentValidation, ProblemDetails  
- Serilog + CorrelationId  
- Rate limiting, Health checks  
- Optimistic concurrency  
- Integration tests  

---

## Architecture
```text
Api → Application → Domain → Infrastructure → Contracts
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
- Logging, error handling  
- Rate limiting (429), concurrency (409)  

---

## Testing
- WebApplicationFactory  
- SQLite in-memory  

---

## Purpose
Modern .NET backend demo.