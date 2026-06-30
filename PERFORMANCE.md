# Performance testing

## SLA

Baseline load tests fail when any scenario exceeds these limits:

- p95 latency: 200 ms
- p99 latency: 500 ms
- error rate: 1%
- orders list throughput: 20 requests/second
- order detail throughput: 10 requests/second
- customers list throughput: 5 requests/second
- order creation throughput: 1 request/second

The thresholds are defined in `LoadTests/PerformanceSla.cs`.

## Run the API

Use the `LoadTesting` environment so write requests are not rejected by the normal production rate limit.

```powershell
$env:ASPNETCORE_ENVIRONMENT="LoadTesting"
dotnet run --project OrderManagement.Api -c Release --urls "http://localhost:5088"
```

## Run load profiles

```powershell
$env:BASE_URL="http://localhost:5088"
$env:LOAD_PROFILE="baseline" # baseline, stress, or spike
dotnet run --project LoadTests -c Release
```

Reports are timestamped and written to `LoadTests/load-test-results` as HTML, TXT, and CSV. Keep the first stable baseline report and compare later runs made on equivalent hardware and data.

## Run microbenchmarks

```powershell
dotnet run --project Benchmarks -c Release
```

For a fast compilation/smoke check:

```powershell
dotnet run --project Benchmarks -c Release -- --job Dry
```

BenchmarkDotNet records execution time and memory allocations for request validation, JSON serialization, and DTO mapping.

## Profile the API under load

Install the diagnostics tools once:

```powershell
dotnet tool install --global dotnet-counters
dotnet tool install --global dotnet-trace
```

Find the API process ID, start a 60-second profiler, and run NBomber at the same time:

```powershell
Get-Process OrderManagement.Api
.\scripts\profile-counters.ps1 -ProcessId <PID> -DurationSeconds 60
.\scripts\profile-trace.ps1 -ProcessId <PID> -DurationSeconds 60
```

Counter CSV and `.nettrace` files are written to `profiling-results`. Open traces in PerfView or Visual Studio Profiler and check CPU hot paths, GC pauses, allocations, exceptions, and blocking I/O before making optimizations.

## CI

Run the `Performance` GitHub Actions workflow manually and provide a reachable staging API URL. The baseline profile acts as the regression gate; stress and spike profiles collect evidence without applying baseline SLA limits. NBomber and BenchmarkDotNet reports are retained as workflow artifacts.
